import type { CurrentUser } from '../shared/types'

function permissions(user: CurrentUser | null): string[] {
  return user?.permissions ?? []
}

export function canReadRoomOperations(user: CurrentUser | null): boolean {
  return permissions(user).includes('room-operations.read')
}

export function canManageRoomOperations(user: CurrentUser | null): boolean {
  return permissions(user).includes('room-operations.manage')
}

export function canInspectRoomOperations(user: CurrentUser | null): boolean {
  return permissions(user).includes('room-operations.inspect')
}
