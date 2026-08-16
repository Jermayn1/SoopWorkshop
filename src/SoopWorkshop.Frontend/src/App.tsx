import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { HomePage } from './pages/HomePage'
import { TaskPage } from './pages/TaskPage'
import { ResultPage } from './pages/ResultPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { RequireAdmin } from './admin/RequireAdmin'
import { AdminHomePage } from './admin/pages/AdminHomePage'

export default function App() {
  return (
    <Routes>
      {/* Jede Ansicht hat eine eigene Adresse. Im Referenzprojekt wurde
          zwischen Aufgabe und Ergebnis nur ein useState umgeschaltet — damit
          gab es keinen teilbaren Link, der Zurueck-Knopf des Browsers fuehrte
          aus der Anwendung heraus, und ein Neuladen verlor das Ergebnis. */}
      <Route element={<AppLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/aufgaben/:taskId" element={<TaskPage />} />
        <Route path="/abgaben/:submissionId" element={<ResultPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>

      {/* Eigener Zweig mit eigenem Rahmen: die Verwaltung teilt sich mit der
          Teilnehmersicht weder Seitenleiste noch Kopfzeile. RequireAdmin zeigt
          davor die Anmeldung, ohne die Adresse zu wechseln. */}
      <Route path="/verwaltung" element={<RequireAdmin />}>
        <Route index element={<AdminHomePage />} />
      </Route>
    </Routes>
  )
}
