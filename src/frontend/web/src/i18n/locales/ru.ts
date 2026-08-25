import type { Translations } from '../types'
import { ru as common } from '../common/ru'
import { ru as auth } from '../auth/ru'
import { ru as workforce } from '../workforce/ru'
import { ru as hr } from '../hr/ru'
import { ru as roomOperations } from '../room-operations/ru'
import { ru as technicalService } from '../technical-service/ru'
import { ru as authorization } from '../authorization/ru'

export const ru: Translations = {
  ...common,
  ...auth,
  ...workforce,
  ...hr,
  ...roomOperations,
  ...technicalService,
  ...authorization,
}
