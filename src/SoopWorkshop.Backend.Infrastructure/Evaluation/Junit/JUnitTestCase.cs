namespace SoopWorkshop.Backend.Infrastructure.Evaluation.Junit
{
    // Eine Testmethode aus dem XML-Report des JUnit-Launchers.
    public sealed record JUnitTestCase(
        string DisplayName,
        string ClassName,
        string MethodName,
        bool Passed,
        string Message);
}
