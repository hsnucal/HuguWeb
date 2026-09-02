import { listHrEmployees, type HrEmployeeListItem } from './hrApi.ts'
import { asCollection, asHrEmployeeList } from './personnelDirectory.ts'
import { listDepartments, listPositions, type DepartmentRecord, type PositionRecord } from './workforceApi.ts'

export type PropertyStructure = {
  departments: DepartmentRecord[]
  positions: PositionRecord[]
}

/** Personnel master list is organization-scoped and must not depend on property context. */
export async function loadPersonnelEmployees(): Promise<HrEmployeeListItem[]> {
  const people = await listHrEmployees()
  return asHrEmployeeList<HrEmployeeListItem>(people)
}

/** Department/position structure requires an active property in the session. */
export async function loadPropertyStructure(): Promise<PropertyStructure> {
  const [departmentRows, positionRows] = await Promise.all([listDepartments(), listPositions()])
  return {
    departments: asCollection<DepartmentRecord>(departmentRows),
    positions: asCollection<PositionRecord>(positionRows),
  }
}
