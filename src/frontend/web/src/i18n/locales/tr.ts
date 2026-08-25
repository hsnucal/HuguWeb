import type { Translations } from '../types'
import { tr as common } from '../common/tr'
import { tr as auth } from '../auth/tr'
import { tr as workforce } from '../workforce/tr'
import { tr as hr } from '../hr/tr'
import { tr as roomOperations } from '../room-operations/tr'
import { tr as technicalService } from '../technical-service/tr'
import { tr as authorization } from '../authorization/tr'

export const tr: Translations = {
  ...common,
  ...auth,
  ...workforce,
  ...hr,
  ...roomOperations,
  ...technicalService,
  ...authorization,
}
