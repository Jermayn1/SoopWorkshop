using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoopWorkshop.Backend.Application.Repositories;
using SoopWorkshop.Backend.Application.Tasks.Services;
using SoopWorkshop.Backend.Domain.Entities;
using SoopWorkshop.Shared.DTOs.Tasks.Requests;
using SoopWorkshop.Shared.Enums;

namespace SoopWorkshop.Tests.Unit.Application.Tasks
{
    public class TaskItemServiceTests
    {
        private readonly ITaskItemRepository _taskItemRepository = Substitute.For<ITaskItemRepository>();
        private readonly ITaskCategoryRepository _categoryRepository = Substitute.For<ITaskCategoryRepository>();

        private TaskItemService CreateService() =>
            new(_taskItemRepository, _categoryRepository, NullLogger<TaskItemService>.Instance);

        private Guid GivenExistingCategory()
        {
            var categoryId = Guid.NewGuid();
            _categoryRepository.ExistsAsync(categoryId, Arg.Any<CancellationToken>()).Returns(true);
            return categoryId;
        }

        private TaskItem GivenExistingTask(Guid categoryId)
        {
            var item = new TaskItem
            {
                Id = Guid.NewGuid(),
                TaskCategoryId = categoryId,
                Title = "Alt",
                Description = "Alte Beschreibung",
                EvaluationMode = EvaluationMode.ConsoleOnly
            };

            _taskItemRepository.GetByIdAsync(item.Id).Returns(item);
            return item;
        }

        [Fact]
        public async Task CreateAsync_KategorieExistiert_LegtAn()
        {
            var categoryId = GivenExistingCategory();

            var result = await CreateService().CreateAsync(new CreateTaskItemDto
            {
                TaskCategoryId = categoryId,
                Title = "Rechner",
                Description = "Addiere zwei Zahlen."
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.Title.ShouldBe("Rechner");
            await _taskItemRepository.Received(1).AddAsync(Arg.Any<TaskItem>());
        }

        // Vorher lief das in die Fremdschluesselbedingung und kam als 500
        // "Ein unerwarteter Fehler ist aufgetreten." zurueck.
        [Fact]
        public async Task CreateAsync_KategorieExistiertNicht_LiefertFehlerUndLegtNichtsAn()
        {
            _categoryRepository.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

            var result = await CreateService().CreateAsync(new CreateTaskItemDto
            {
                TaskCategoryId = Guid.NewGuid(),
                Title = "Rechner",
                Description = "Addiere zwei Zahlen."
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Kategorie");
            await _taskItemRepository.DidNotReceive().AddAsync(Arg.Any<TaskItem>());
        }

        // Umhaengen war ueber die API gar nicht moeglich, weil UpdateTaskItemDto
        // kein TaskCategoryId hatte.
        [Fact]
        public async Task UpdateAsync_AndereKategorie_HaengtDieAufgabeUm()
        {
            var alteKategorie = GivenExistingCategory();
            var item = GivenExistingTask(alteKategorie);
            var neueKategorie = GivenExistingCategory();

            var result = await CreateService().UpdateAsync(new UpdateTaskItemDto
            {
                Id = item.Id,
                TaskCategoryId = neueKategorie,
                Title = "Rechner",
                Description = "Addiere zwei Zahlen."
            });

            result.IsSuccess.ShouldBeTrue();
            result.Value!.TaskCategoryId.ShouldBe(neueKategorie);
            item.TaskCategoryId.ShouldBe(neueKategorie);
        }

        [Fact]
        public async Task UpdateAsync_KategorieExistiertNicht_LiefertFehlerUndAendertNichts()
        {
            var alteKategorie = GivenExistingCategory();
            var item = GivenExistingTask(alteKategorie);

            var result = await CreateService().UpdateAsync(new UpdateTaskItemDto
            {
                Id = item.Id,
                TaskCategoryId = Guid.NewGuid(),
                Title = "Neuer Titel",
                Description = "Neue Beschreibung"
            });

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Kategorie");
            item.Title.ShouldBe("Alt");
            await _taskItemRepository.DidNotReceive().UpdateAsync(Arg.Any<TaskItem>());
        }

        // Regressionswache fuer einen 500er, der jede Aenderung an einer Aufgabe
        // mit Tipps oder Signaturen getroffen hat.
        //
        // Beim Aendern ist die Aufgabe bereits von EF verfolgt. An einem gesetzten
        // Schluessel erkennt die Aenderungsverfolgung eine BESTEHENDE Zeile und
        // schickt ein UPDATE auf etwas, das es nicht gibt -> null betroffene
        // Zeilen -> DbUpdateConcurrencyException. Neue Kinder muessen deshalb
        // ohne Id in die Sammlung wandern.
        //
        // Den Fehler selbst kann dieser Test nicht ausloesen - dazu braeuchte er
        // eine echte Datenbank (Phase 6, §10.5). Er haelt aber die Ursache fest.
        [Fact]
        public async Task UpdateAsync_NeueTippsUndKlassen_KommenOhneVorgegebeneIdInDieSammlung()
        {
            var item = GivenExistingTask(GivenExistingCategory());

            var result = await CreateService().UpdateAsync(new UpdateTaskItemDto
            {
                Id = item.Id,
                TaskCategoryId = item.TaskCategoryId,
                Title = "Rechner",
                Description = "Addiere zwei Zahlen.",
                Hints = ["Denk an negative Zahlen."],
                ExpectedTypes =
                [
                    new ExpectedTypeInputDto
                    {
                        Name = "Rechner",
                        Methods = ["public static int addiere(int a, int b)"]
                    }
                ]
            });

            result.IsSuccess.ShouldBeTrue();

            item.Hints.ShouldHaveSingleItem().Id.ShouldBe(Guid.Empty);

            var type = item.ExpectedTypes.ShouldHaveSingleItem();
            type.Id.ShouldBe(Guid.Empty);
            type.Methods.ShouldHaveSingleItem().Id.ShouldBe(Guid.Empty);
        }

        // Der Methodenname wird aus der Signatur abgeleitet, damit der Admin ihn
        // nicht zweimal aufschreiben muss.
        [Fact]
        public async Task UpdateAsync_MehrereKlassen_UebernimmtStrukturUndLeitetNamenAb()
        {
            var item = GivenExistingTask(GivenExistingCategory());

            var result = await CreateService().UpdateAsync(new UpdateTaskItemDto
            {
                Id = item.Id,
                TaskCategoryId = item.TaskCategoryId,
                Title = "Bankkonto",
                Description = "Konto und Kunde.",
                ExpectedTypes =
                [
                    new ExpectedTypeInputDto { Name = "Konto", Methods = ["public void einzahlen(double betrag)"] },
                    new ExpectedTypeInputDto { Name = "Kunde", Methods = [] }
                ]
            });

            result.IsSuccess.ShouldBeTrue();

            item.ExpectedTypes.Count.ShouldBe(2);

            var konto = item.ExpectedTypes.First();
            konto.Name.ShouldBe("Konto");
            konto.Order.ShouldBe(1);
            konto.Methods.ShouldHaveSingleItem().Name.ShouldBe("einzahlen");

            var kunde = item.ExpectedTypes.Last();
            kunde.Name.ShouldBe("Kunde");
            kunde.Order.ShouldBe(2);
            kunde.Methods.ShouldBeEmpty();

            result.Value!.ExpectedTypes.Select(type => type.Name).ShouldBe(["Konto", "Kunde"]);
        }

        [Fact]
        public async Task ToggleVisibilityAsync_ModusVerlangtTestfaelleDieFehlen_LehntAb()
        {
            var item = GivenExistingTask(GivenExistingCategory());
            item.EvaluationMode = EvaluationMode.ConsoleOnly;
            item.IsVisible = false;

            var result = await CreateService().ToggleVisibilityAsync(item.Id);

            result.IsSuccess.ShouldBeFalse();
            result.ErrorMessage.ShouldContain("Konsolen-Testfall");
            item.IsVisible.ShouldBeFalse();
        }

        // Ausschalten ist immer erlaubt — die Pruefung gilt nur fuers Freischalten.
        [Fact]
        public async Task ToggleVisibilityAsync_SichtbareAufgabeOhneTestdaten_LaesstSichVerbergen()
        {
            var item = GivenExistingTask(GivenExistingCategory());
            item.EvaluationMode = EvaluationMode.ConsoleOnly;
            item.IsVisible = true;

            var result = await CreateService().ToggleVisibilityAsync(item.Id);

            result.IsSuccess.ShouldBeTrue();
            item.IsVisible.ShouldBeFalse();
        }
    }
}
