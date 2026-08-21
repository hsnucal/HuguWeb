import { Navigate, Route, Routes } from 'react-router'
import { AppShell } from './AppShell'
import { ProtectedRoute } from './ProtectedRoute'
import { LoginPage } from '../auth/LoginPage'
import { OperationsCenter } from './OperationsCenter'
import { ActiveWorkforcePage } from '../workforce/ActiveWorkforcePage'
import { DepartmentsPage } from '../workforce/DepartmentsPage'
import { EmployeeDetailPage } from '../workforce/EmployeeDetailPage'
import { PositionsPage } from '../workforce/PositionsPage'
import { WorkforceLayout } from '../workforce/WorkforceLayout'

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/app"
        element={
          <ProtectedRoute>
            <AppShell />
          </ProtectedRoute>
        }
      >
        <Route index element={<OperationsCenter />} />
        <Route path="workforce" element={<WorkforceLayout />}>
          <Route index element={<ActiveWorkforcePage />} />
          <Route path="departments" element={<DepartmentsPage />} />
          <Route path="positions" element={<PositionsPage />} />
          <Route path="employees/:employeeId" element={<EmployeeDetailPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/app" replace />} />
    </Routes>
  )
}
