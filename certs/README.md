# certs

Hierhin gehoeren das Zertifikat und der Schluessel fuer den Reverse Proxy:

```
certs/soop.pem       Zertifikat
certs/soop-key.pem   privater Schluessel
```

Beide werden in den Frontend-Container gemountet (schreibgeschuetzt) und sind
**gitignoriert** — ein privater Schluessel gehoert in kein Repository.

Wie sie erzeugt und auf die Teilnehmerrechner verteilt werden, steht in
[docs/server-aufsetzen.md](../docs/server-aufsetzen.md), Abschnitt 6.

Liegen sie nicht hier, startet das Frontend trotzdem und erzeugt sich ein
selbstsigniertes Zertifikat — dann zeigt aber jeder Browser eine Warnung. Der
Container sagt das beim Start deutlich.
