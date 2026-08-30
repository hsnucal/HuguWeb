import { Navigate, Route, Routes } from 'react-router'
import { AppShell } from './AppShell'
import { ProtectedRoute } from './ProtectedRoute'
import { LoginPage } from '../auth/LoginPage'
import { OperationsCenter } from './OperationsCenter'
import { ActiveWorkforcePage } from '../workforce/ActiveWorkforcePage'
import { DepartmentsPage } from '../workforce/DepartmentsPage'
import { PositionsPage } from '../workforce/PositionsPage'
import { SgkWorkplacesPage } from '../workforce/SgkWorkplacesPage'
import { LeaveTypesPage } from '../workforce/LeaveTypesPage'
import { ShiftDefinitionsPage } from '../workforce/ShiftDefinitionsPage'
import { ShiftPlanPage } from '../workforce/ShiftPlanPage'
import { WorkforceLayout } from '../workforce/WorkforceLayout'
import { AuthorizationLayout } from '../authorization/AuthorizationLayout'
import { UsersPage } from '../authorization/UsersPage'
import { RolesPage } from '../authorization/RolesPage'
import { RoomOperationsPage } from '../room-operations/RoomOperationsPage'
import { RoomDetailPage } from '../room-operations/RoomDetailPage'
import { TechnicalServicePage } from '../technical-service/TechnicalServicePage'
import { CreateIssuePage } from '../technical-service/CreateIssuePage'
import { IssueDetailPage } from '../technical-service/IssueDetailPage'

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
        <Route path="room-operations" element={<RoomOperationsPage />} />
        <Route path="room-operations/:roomId" element={<RoomDetailPage />} />
        <Route path="technical-service" element={<TechnicalServicePage />} />
        <Route path="technical-service/new" element={<CreateIssuePage />} />
        <Route path="technical-service/:issueId" element={<IssueDetailPage />} />
        <Route path="workforce" element={<WorkforceLayout />}>
          <Route index element={<ActiveWorkforcePage />} />
          <Route path="departments" element={<DepartmentsPage />} />
          <Route path="positions" element={<PositionsPage />} />
          <Route path="official-settings" element={<SgkWorkplacesPage />} />
          <Route path="leave-types" element={<LeaveTypesPage />} />
          <Route path="shift-definitions" element={<ShiftDefinitionsPage />} />
          <Route path="shift-plan" element={<ShiftPlanPage />} />
        </Route>
        <Route path="settings" element={<AuthorizationLayout />}>
          <Route index element={<Navigate to="users" replace />} />
          <Route path="users" element={<UsersPage />} />
          <Route path="roles" element={<RolesPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/app" replace />} />
    </Routes>
  )
}
