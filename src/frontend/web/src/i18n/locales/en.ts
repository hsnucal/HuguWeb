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
    active: string
    activeIntro: string
    departments: string
    departmentsIntro: string
    positions: string
    positionsIntro: string
    hire: string
    transfer: string
    endEmployment: string
    personnelNumber: string
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
    emptyActive: string
    emptyDepartments: string
    emptyPositions: string
    scheduled: string
    former: string
    currentAssignment: string
    employment: string
    assignmentHistory: string
    noHistory: string
    selectDepartment: string
    selectPosition: string
    confirmEnd: string
    workingIn: string
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
    active: 'Active staff',
    activeIntro: 'People currently employed at this property.',
    departments: 'Departments',
    departmentsIntro: 'Hotel structure used when hiring and transferring staff.',
    positions: 'Positions',
    positionsIntro:
      'Job titles used in assignments. A position is not owned by a department and does not grant system permissions.',
    hire: 'Hire employee',
    transfer: 'Transfer',
    endEmployment: 'End employment',
    personnelNumber: 'Personnel number',
    givenName: 'Given name',
    familyName: 'Family name',
    startDate: 'Employment start date',
    effectiveDate: 'Effective date',
    endDate: 'Employment end date',
    department: 'Department',
    position: 'Position',
    name: 'Name',
    code: 'Code',
    status: 'Status',
    activeStatus: 'Active',
    scheduledStatus: 'Scheduled',
    endedStatus: 'Ended',
    inactive: 'Inactive',
    activate: 'Activate',
    deactivate: 'Deactivate',
    createDepartment: 'Add department',
    createPosition: 'Add position',
    rename: 'Rename',
    save: 'Save',
    cancel: 'Cancel',
    emptyActive: 'No one is currently working here. Hire an employee to start the workforce list.',
    emptyDepartments: 'No departments yet. Add the hotel structure before hiring.',
    emptyPositions: 'No positions yet. Add a job title before hiring.',
    scheduled: 'Starting later',
    former: 'Former staff',
    currentAssignment: 'Current assignment',
    employment: 'Employment',
    assignmentHistory: 'Assignment history',
    noHistory: 'No assignment history yet.',
    selectDepartment: 'Select a department',
    selectPosition: 'Select a position',
    confirmEnd: 'This ends the employment relationship. The person and history remain in HuGuWeb.',
    workingIn: '{{department}} · {{position}}',
    errors: {
      personnelNumberInUse: 'This personnel number is already in use, including for former staff.',
      departmentInactive: 'Choose an active department.',
      positionInactive: 'Choose an active position.',
      employmentEnded: 'This employment has already ended.',
      noCurrentEmployment: 'This person does not have a current employment.',
      invalidTransferDate: 'The transfer date would overlap or invert assignment history.',
      overlappingPrimaryAssignment: 'Primary assignments cannot overlap.',
      invalidEmploymentPeriod: 'The end date must be on or after the employment start date.',
      sameAssignment: 'This person already works in that department and position.',
      generic: 'The workforce request could not be completed.',
    },
  },
}
