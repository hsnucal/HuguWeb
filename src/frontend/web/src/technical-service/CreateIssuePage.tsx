import { useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthSession } from '../auth/AuthContext'
import { Button } from '../ui/Button'
import { ChevronLeftIcon } from '../ui/icons'
import { Notice } from '../ui/Notice'
import { SelectField } from '../ui/SelectField'
import { Surface } from '../ui/Surface'
import { TextArea } from '../ui/TextField'
import { canManageMaintenance } from './maintenanceAccess'
import {
  createIssue,
  listAssignableEmployees,
  listCategories,
  listRooms,
  maintenanceErrorKey,
  type AssignableEmployeeItem,
  type MaintenanceCategoryItem,
  type MaintenancePriority,
  type MaintenanceRoomItem,
  type OutageClassification,
} from './maintenanceApi'
import styles from './TechnicalService.module.css'

export function CreateIssuePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const { user } = useAuthSession()
  const canManage = canManageMaintenance(user)
  const [rooms, setRooms] = useState<MaintenanceRoomItem[]>([])
  const [categories, setCategories] = useState<MaintenanceCategoryItem[]>([])
  const [employees, setEmployees] = useState<AssignableEmployeeItem[]>([])
  const [roomId, setRoomId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<MaintenancePriority>('Normal')
  const [employeeId, setEmployeeId] = useState('')
  const [blocksRoomUse, setBlocksRoomUse] = useState(false)
  const [outage, setOutage] = useState<OutageClassification>('OutOfOrder')
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!canManage) {
      return
    }

    let cancelled = false

    async function load() {
      try {
        const [roomRows, categoryRows, people] = await Promise.all([
          listRooms(),
          listCategories(),
          listAssignableEmployees(),
        ])
        if (cancelled) {
          return
        }

        setRooms(roomRows)
        setCategories(categoryRows)
        setEmployees(people)
        setRoomId((current) => current || roomRows[0]?.roomId || '')
        setCategoryId((current) => current || categoryRows[0]?.id || '')
      } catch (reason) {
        if (!cancelled) {
          setError(t(maintenanceErrorKey(reason)))
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [canManage, t])

  if (!canManage) {
    return <Notice tone="danger">{t('maintenance.noManage')}</Notice>
  }

  async function onSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSaving(true)
    try {
      const created = await createIssue({
        roomId,
        categoryId,
        description,
        priority,
        assignedEmployeeId: employeeId || undefined,
        blocksRoomUse,
        outageClassification: blocksRoomUse ? outage : undefined,
      })
      navigate(`/app/technical-service/${created.id}`, { replace: true })
    } catch (reason) {
      setError(t(maintenanceErrorKey(reason)))
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className={styles.page}>
      <Link to="/app/technical-service" className={styles.backLink}>
        <ChevronLeftIcon />
        {t('maintenance.back')}
      </Link>

      {error ? <Notice tone="danger">{error}</Notice> : null}

      <Surface tone="panel">
        <h2 className={styles.sectionTitle}>{t('maintenance.create')}</h2>
        <form className={styles.form} onSubmit={(event) => void onSubmit(event)}>
          <div className={styles.formGrid}>
            <SelectField
              id="maintenance-room"
              label={t('maintenance.room')}
              value={roomId}
              onChange={setRoomId}
              required
            >
              {rooms.map((room) => (
                <option key={room.roomId} value={room.roomId}>
                  {room.number}
                </option>
              ))}
            </SelectField>
            <SelectField
              id="maintenance-category"
              label={t('maintenance.category')}
              value={categoryId}
              onChange={setCategoryId}
              required
            >
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </SelectField>
            <div className={styles.wide}>
              <TextArea
                id="maintenance-description"
                label={t('maintenance.issue')}
                value={description}
                onChange={setDescription}
                required
              />
            </div>
            <SelectField
              id="maintenance-priority"
              label={t('maintenance.priorityLabel')}
              value={priority}
              onChange={(value) => setPriority(value as MaintenancePriority)}
            >
              <option value="Normal">{t('maintenance.priority.Normal')}</option>
              <option value="High">{t('maintenance.priority.High')}</option>
              <option value="Urgent">{t('maintenance.priority.Urgent')}</option>
            </SelectField>
            <SelectField
              id="maintenance-employee"
              label={t('maintenance.assigned')}
              value={employeeId}
              onChange={setEmployeeId}
            >
              <option value="">{t('maintenance.unassigned')}</option>
              {employees.map((person) => (
                <option key={person.employeeId} value={person.employeeId}>
                  {person.displayName}
                </option>
              ))}
            </SelectField>
            <SelectField
              id="maintenance-blocking"
              label={t('maintenance.blocksRoomUse')}
              value={blocksRoomUse ? 'yes' : 'no'}
              onChange={(value) => setBlocksRoomUse(value === 'yes')}
            >
              <option value="no">{t('maintenance.blocksNo')}</option>
              <option value="yes">{t('maintenance.blocksYes')}</option>
            </SelectField>
            {blocksRoomUse ? (
              <SelectField
                id="maintenance-outage"
                label={t('maintenance.outageLabel')}
                value={outage}
                onChange={(value) => setOutage(value as OutageClassification)}
              >
                <option value="OutOfOrder">{t('maintenance.outage.OutOfOrder')}</option>
                <option value="OutOfService">{t('maintenance.outage.OutOfService')}</option>
              </SelectField>
            ) : null}
          </div>
          <div className={styles.formFooter}>
            <Button type="submit" loading={saving} disabled={!roomId || !categoryId || !description.trim()}>
              {t('maintenance.createSubmit')}
            </Button>
          </div>
        </form>
      </Surface>
    </div>
  )
}
