import { Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout'
import { HomePage } from './pages/HomePage'
import { TaskPage } from './pages/TaskPage'
import { ResultPage } from './pages/ResultPage'
import { NotFoundPage } from './pages/NotFoundPage'

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
    </Routes>
  )
}
