import type { Translations } from '../types'
import { en as common } from '../common/en'
import { en as auth } from '../auth/en'
import { en as workforce } from '../workforce/en'
import { en as hr } from '../hr/en'
import { en as roomOperations } from '../room-operations/en'
import { en as technicalService } from '../technical-service/en'
import { en as authorization } from '../authorization/en'

export const en: Translations = {
  ...common,
  ...auth,
  ...workforce,
  ...hr,
  ...roomOperations,
  ...technicalService,
  ...authorization,
}
