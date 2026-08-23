export type Translations = {
  common: {
    language: string
    skipToContent: string
    preferenceSaveFailed: string
    preferenceSaveFailedKeep: string
  }
  auth: {
    welcomeBack: string
    signInToContinue: string
    hotelOperations: string
    email: string
    password: string
    signIn: string
    signingIn: string
    signInFailed: string
    checkingSession: string
    signedIn: string
    signOut: string
  }
  navigation: {
    application: string
    primary: string
    home: string
    rooms: string
    reservations: string
    tasks: string
    settings: string
    unavailable: string
    workforce: string
    roomOperations: string
  }
  operations: {
    title: string
    intro: string
    today: string
    arrivals: string
    arrivalsDetail: string
    departures: string
    departuresDetail: string
    roomsNotReady: string
    roomsNotReadyDetail: string
    requiresAttention: string
    roomOperations: string
    upcoming: string
    room: string
    arrivalNotReady: string
    arrivalNotReadyReason: string
    inspectionWaiting: string
    inspectionWaitingReason: string
    maintenanceBlocking: string
    maintenanceBlockingReason: string
    timeSensitive: string
    waiting: string
    blocking: string
    dirty: string
    cleaning: string
    inspection: string
    ready: string
    groupArrival: string
    vipArrival: string
  }
  workforce: {
    title: string
    intro: string
    directory: string
    active: string
    activeIntro: string
    scheduled: string
    scheduledIntro: string
    former: string
    formerIntro: string
    tabCount: string
    departments: string
    departmentsIntro: string
    positions: string
    positionsIntro: string
    hireNew: string
    hireSubmit: string
    hire: string
    transfer: string
    transferSubmit: string
    transferIntro: string
    endEmployment: string
    endEmploymentSubmit: string
    personnelNumber: string
    fullName: string
    givenName: string
    familyName: string
    startDate: string
    effectiveDate: string
    endDate: string
    department: string
    position: string
    name: string
    code: string
    status: string
    activeStatus: string
    scheduledStatus: string
    endedStatus: string
    inactive: string
    activate: string
    deactivate: string
    createDepartment: string
    createPosition: string
    rename: string
    save: string
    cancel: string
    search: string
    searchPlaceholder: string
    allDepartments: string
    loading: string
    emptyActive: string
    emptyScheduled: string
    emptyFormer: string
    emptySearch: string
    emptyDepartments: string
    emptyPositions: string
    hireNeedsStructure: string
    personalSection: string
    startSection: string
    placementSection: string
    currentWork: string
    lastWork: string
    workHistory: string
    noHistory: string
    present: string
    currentDepartment: string
    currentPosition: string
    newDepartment: string
    newPosition: string
    selectDepartment: string
    selectPosition: string
    confirmEnd: string
    backToDirectory: string
    noAccess: string
    errors: {
      personnelNumberInUse: string
      departmentInactive: string
      positionInactive: string
      employmentEnded: string
      noCurrentEmployment: string
      invalidTransferDate: string
      overlappingPrimaryAssignment: string
      invalidEmploymentPeriod: string
      sameAssignment: string
      generic: string
    }
  }
  roomOperations: {
    title: string
    intro: string
    room: string
    readinessLabel: string
    assignedEmployee: string
    priorityLabel: string
    workState: string
    actionNeeded: string
    unassigned: string
    noPriority: string
    noWork: string
    rowSummary: string
    rejectionHint: string
    loading: string
    empty: string
    noAccess: string
    back: string
    noEmployees: string
    needsCleaningTitle: string
    needsCleaningIntro: string
    needsCleaningSubmit: string
    completeTitle: string
    completeIntro: string
    completeSubmit: string
    inspectTitle: string
    inspectIntro: string
    accept: string
    reject: string
    rejectionReason: string
    readinessHistory: string
    inspectionHistory: string
    noHistory: string
    noInspections: string
    readiness: { Dirty: string; Clean: string; Inspected: string; Ready: string }
    priority: { Normal: string; High: string; Urgent: string }
    work: { Open: string; Completed: string }
    needed: { 'needs-cleaning': string; 'complete-cleaning': string; inspect: string; none: string }
    cause: {
      Seeded: string
      NeedsCleaning: string
      CleaningCompleted: string
      InspectionAccepted: string
      InspectionRejected: string
    }
    inspectionResult: { Accepted: string; Rejected: string }
    errors: {
      roomNotFound: string
      employeeNotFound: string
      invalidTransition: string
      activeWork: string
      staleWork: string
      workNotCurrent: string
      rejectionRequired: string
      inspectionNotAllowed: string
      assignmentRequired: string
      generic: string
    }
  }
}

export const en: Translations = {
  common: {
    language: 'Language',
    skipToContent: 'Skip to content',
    preferenceSaveFailed: 'Language preference could not be saved. The previous language was restored.',
    preferenceSaveFailedKeep:
      'Language preference could not be saved. You can try again from the language selector.',
  },
  auth: {
    welcomeBack: 'Welcome back',
    signInToContinue: 'Sign in to continue to HuGuWeb.',
    hotelOperations: 'Hotel operations, in one calm workspace.',
    email: 'Email',
    password: 'Password',
    signIn: 'Sign in',
    signingIn: 'Signing in…',
    signInFailed: 'Sign-in failed. Check your details and try again.',
    checkingSession: 'Checking session…',
    signedIn: 'Signed in',
    signOut: 'Sign out',
  },
  navigation: {
    application: 'Application',
    primary: 'Primary',
    home: 'Home',
    rooms: 'Rooms',
    reservations: 'Reservations',
    tasks: 'Tasks',
    settings: 'Settings',
    unavailable: '{{label}}, not available yet',
    workforce: 'Workforce',
    roomOperations: 'Room Operations',
  },
  operations: {
    title: 'Operations Center',
    intro: "Here's what needs your attention today.",
    today: 'Today',
    arrivals: 'Arrivals',
    arrivalsDetail: 'Peak around {{time}}',
    departures: 'Departures',
    departuresDetail: 'Until {{time}}',
    roomsNotReady: 'Rooms not ready',
    roomsNotReadyDetail: '{{count}} due before next arrival',
    requiresAttention: 'Requires attention',
    roomOperations: 'Room operations',
    upcoming: 'Upcoming',
    room: 'Room {{number}}',
    arrivalNotReady: 'Arrival approaching — room not ready',
    arrivalNotReadyReason: 'Guest arrival at {{time}} · cleaning delayed',
    inspectionWaiting: 'Supervisor inspection waiting',
    inspectionWaitingReason: 'Cleaning finished · waiting on supervisor',
    maintenanceBlocking: 'Maintenance issue blocking readiness',
    maintenanceBlockingReason: 'Open fault · room cannot be prepared',
    timeSensitive: 'Time-sensitive',
    waiting: 'Waiting',
    blocking: 'Blocking',
    dirty: 'Dirty',
    cleaning: 'Cleaning',
    inspection: 'Inspection',
    ready: 'Ready',
    groupArrival: 'Group arrival · {{count}} rooms',
    vipArrival: 'VIP arrival · {{room}}',
  },
  workforce: {
    title: 'Workforce',
    intro: 'Who works here, where they work, and in which position.',
    directory: 'Staff',
    active: 'Active staff',
    activeIntro: 'People currently working at this property.',
    scheduled: 'Scheduled starts',
    scheduledIntro: 'People with a start date in the future.',
    former: 'Former staff',
    formerIntro: 'People who have left. Their records and history remain.',
    tabCount: '{{label}} ({{count}})',
    departments: 'Departments',
    departmentsIntro: 'Hotel structure used when hiring and changing duties. Departments do not grant system permissions.',
    positions: 'Positions',
    positionsIntro:
      'Job titles used when placing staff. A position belongs to the property, not to a department, and does not grant system permissions.',
    hireNew: 'New staff member',
    hireSubmit: 'Hire',
    hire: 'Hire employee',
    transfer: 'Change of duties',
    transferSubmit: 'Save change of duties',
    transferIntro:
      'The current department and position stay in history. From the effective date, the person works in the new department and position.',
    endEmployment: 'End employment',
    endEmploymentSubmit: 'Confirm end of employment',
    personnelNumber: 'Personnel number',
    fullName: 'Name',
    givenName: 'Given name',
    familyName: 'Family name',
    startDate: 'Start date',
    effectiveDate: 'Effective date',
    endDate: 'Last working day',
    department: 'Department',
    position: 'Position',
    name: 'Name',
    code: 'Code',
    status: 'Status',
    activeStatus: 'Active',
    scheduledStatus: 'Scheduled',
    endedStatus: 'Left',
    inactive: 'Inactive',
    activate: 'Activate',
    deactivate: 'Deactivate',
    createDepartment: 'Add department',
    createPosition: 'Add position',
    rename: 'Rename',
    save: 'Save',
    cancel: 'Cancel',
    search: 'Search',
    searchPlaceholder: 'Name or personnel number',
    allDepartments: 'All departments',
    loading: 'Loading staff…',
    emptyActive: 'No staff members yet.',
    emptyScheduled: 'No scheduled starts.',
    emptyFormer: 'No former staff yet.',
    emptySearch: 'No staff match this search.',
    emptyDepartments: 'No departments yet. Add the hotel structure before hiring.',
    emptyPositions: 'No positions yet. Add a job title before hiring.',
    hireNeedsStructure: 'Add at least one active department and one active position before hiring.',
    personalSection: 'Personal details',
    startSection: 'Start of work',
    placementSection: 'Department and position',
    currentWork: 'Current work',
    lastWork: 'Last work',
    workHistory: 'Work history',
    noHistory: 'No previous department or position changes yet.',
    present: 'Present',
    currentDepartment: 'Current department',
    currentPosition: 'Current position',
    newDepartment: 'New department',
    newPosition: 'New position',
    selectDepartment: 'Select a department',
    selectPosition: 'Select a position',
    confirmEnd:
      'This ends employment. The staff record is not deleted, history is kept, and the person leaves the active staff list.',
    backToDirectory: 'Back to staff list',
    noAccess: 'You do not have access to staff records.',
    errors: {
      personnelNumberInUse: 'This personnel number is already in use, including for former staff.',
      departmentInactive: 'Choose an active department.',
      positionInactive: 'Choose an active position.',
      employmentEnded: 'This employment has already ended.',
      noCurrentEmployment: 'This person does not have a current employment.',
      invalidTransferDate: 'A change of duties takes effect after the current duty has started. Choose a later date.',
      overlappingPrimaryAssignment: 'Primary assignments cannot overlap.',
      invalidEmploymentPeriod: 'The end date must be on or after the employment start date.',
      sameAssignment: 'This person already works in that department and position.',
      generic: 'The workforce request could not be completed.',
    },
  },
  roomOperations: {
    title: 'Room Operations',
    intro: 'Which rooms need readiness attention now.',
    room: 'Room',
    readinessLabel: 'Readiness',
    assignedEmployee: 'Assigned',
    priorityLabel: 'Priority',
    workState: 'Work status',
    actionNeeded: 'Next action',
    unassigned: 'Unassigned',
    noPriority: '—',
    noWork: 'No current work',
    rowSummary:
      'Room {{number}}. Readiness {{readiness}}. {{person}}. Priority {{priority}}. {{work}}. {{action}}.',
    rejectionHint: 'Required when rejecting the room.',
    loading: 'Loading rooms…',
    empty: 'No rooms are available yet.',
    noAccess: 'You do not have access to room operations.',
    back: 'Back to room operations',
    noEmployees: 'No currently employed people are available to assign. Hire staff in Workforce first.',
    needsCleaningTitle: 'Needs cleaning',
    needsCleaningIntro: 'The room is being sent to cleaning. This is not checkout.',
    needsCleaningSubmit: 'Needs cleaning',
    completeTitle: 'Cleaning complete',
    completeIntro: 'Mark the current housekeeping work complete. The room becomes Clean, not Ready.',
    completeSubmit: 'Cleaning complete',
    inspectTitle: 'Inspection',
    inspectIntro: 'Accept only after a physical inspection. A rejection reason is required.',
    accept: 'Approve room',
    reject: 'Find incomplete / reject',
    rejectionReason: 'Rejection reason',
    readinessHistory: 'Readiness history',
    inspectionHistory: 'Inspection history',
    noHistory: 'No readiness history yet.',
    noInspections: 'No inspections yet.',
    readiness: { Dirty: 'Dirty', Clean: 'Clean', Inspected: 'Inspected', Ready: 'Ready' },
    priority: { Normal: 'Normal', High: 'High', Urgent: 'Urgent' },
    work: { Open: 'Open', Completed: 'Completed' },
    needed: {
      'needs-cleaning': 'Needs cleaning',
      'complete-cleaning': 'Complete cleaning',
      inspect: 'Inspection required',
      none: 'No action',
    },
    cause: {
      Seeded: 'Initial room',
      NeedsCleaning: 'Needs cleaning',
      CleaningCompleted: 'Cleaning completed',
      InspectionAccepted: 'Inspection accepted',
      InspectionRejected: 'Inspection rejected',
    },
    inspectionResult: { Accepted: 'Accepted', Rejected: 'Rejected' },
    errors: {
      roomNotFound: 'The room was not found.',
      employeeNotFound: 'The employee was not found or is not currently employed.',
      invalidTransition: 'That preparation step is not allowed for this room now.',
      activeWork: 'This room already has current housekeeping work.',
      staleWork: 'This work is no longer current and cannot change the room.',
      workNotCurrent: 'This housekeeping work is not the current work for the room.',
      rejectionRequired: 'A rejection reason is required.',
      inspectionNotAllowed: 'Inspection is only allowed when the room is Clean.',
      assignmentRequired: 'Choose an assigned employee.',
      generic: 'The room operations request could not be completed.',
    },
  },
}
