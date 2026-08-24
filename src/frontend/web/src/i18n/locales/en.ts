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
    technicalService: string
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
    distribution: string
    roomsInSnapshot: string
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
    applicableDepartments: string
    rename: string
    save: string
    cancel: string
    search: string
    searchPlaceholder: string
    allDepartments: string
    loading: string
    emptyActive: string
    emptyActiveHint: string
    emptyScheduled: string
    emptyScheduledHint: string
    emptyFormer: string
    emptyFormerHint: string
    emptySearch: string
    emptySearchHint: string
    emptyDepartments: string
    emptyDepartmentsHint: string
    emptyPositions: string
    emptyPositionsHint: string
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
    selectDepartmentFirst: string
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
      positionNotAvailable: string
      generic: string
    }
  }
  personnel: {
    newPersonnel: string
    cardTitle: string
    cardTitleCreate: string
    close: string
    closeCard: string
    cancel: string
    save: string
    saving: string
    createSuccess: string
    saveSuccess: string
    photo: string
    photoHint: string
    uploadPhoto: string
    replacePhoto: string
    removePhoto: string
    organization: string
    property: string
    notes: string
    columns: string
    columnPicker: string
    columnFixed: string
    columnOptional: string
    allPositions: string
    allStatuses: string
    startFrom: string
    startTo: string
    filters: string
    tabGeneral: string
    tabIdentity: string
    tabWork: string
    tabHistory: string
    sectionIdentity: string
    sectionContact: string
    sectionAddress: string
    sectionEmergency: string
    identityScheme: string
    identityNumber: string
    nationality: string
    gender: string
    birthDate: string
    birthPlace: string
    maritalStatus: string
    bloodType: string
    educationLevel: string
    mobilePhone: string
    homePhone: string
    email: string
    residenceAddress: string
    city: string
    district: string
    notificationAddress: string
    emergencyName: string
    emergencyRelationship: string
    emergencyPhone: string
    emergencyPrimary: string
    addEmergency: string
    removeEmergency: string
    noEmergency: string
    placeholders: {
      givenName: string
      familyName: string
      email: string
      birthPlace: string
      educationLevel: string
      bloodType: string
      identityScheme: string
      nationality: string
      maritalStatus: string
      gender: string
      identityNumber: string
      homePhone: string
      city: string
      district: string
      residenceAddress: string
      notificationAddress: string
      notes: string
      emergencyName: string
      emergencyRelationship: string
      emergencyPhone: string
    }
    schemeNone: string
    schemeTckn: string
    schemeYkn: string
    schemePassport: string
    schemeOther: string
    unspecified: string
    genderFemale: string
    genderMale: string
    maritalSingle: string
    maritalMarried: string
    maritalDivorced: string
    maritalWidowed: string
    educationPrimary: string
    educationSecondary: string
    educationHighSchool: string
    educationAssociate: string
    educationBachelor: string
    educationMaster: string
    educationDoctorate: string
    dirtyTitle: string
    dirtyBody: string
    dirtyContinue: string
    dirtyDiscard: string
    personnelNumberAuto: string
    personnelNumberReadOnly: string
    noPositionsForDepartment: string
    createNeedsStructure: string
    historyEmptyCreate: string
    employmentPeriod: string
    assignmentPeriod: string
    transferAction: string
    noHrAccess: string
    sensitiveHidden: string
    errors: {
      nationalIdentityInUse: string
      invalidHrProfile: string
      invalidEmergencyContact: string
      invalidPhoto: string
      sensitiveWriteForbidden: string
      generic: string
      fixFields: string
    }
    validation: {
      tcknLength: string
      tcknInvalid: string
      yknFormat: string
      passportFormat: string
      identitySchemeRequired: string
      identityTooLong: string
      identityInvalid: string
      phoneInvalid: string
      homePhoneInvalid: string
      emergencyPhoneInvalid: string
      phoneRequired: string
      mobilePhoneLength: string
      emailInvalid: string
      emailTooLong: string
      birthDateInvalid: string
      textTooLong: string
      givenNameRequired: string
      givenNameTooLong: string
      familyNameRequired: string
      familyNameTooLong: string
      personnelNumberRequired: string
      personnelNumberTooLong: string
      emergencyNameRequired: string
      emergencyNameTooLong: string
      emergencyPrimaryMultiple: string
      departmentRequired: string
      positionRequired: string
      startDateRequired: string
      positionNotAvailable: string
    }
  }
  roomOperations: {
    title: string
    intro: string
    room: string
    readinessLabel: string
    technicalCondition: string
    activeTechnicalIssue: string
    viewTechnicalIssue: string
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
    emptyHint: string
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
    serviceability: { Serviceable: string; OutOfOrder: string; OutOfService: string }
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
  maintenance: {
    title: string
    intro: string
    create: string
    createIntro: string
    createSubmit: string
    detailTitle: string
    detailIntro: string
    room: string
    issue: string
    category: string
    priorityLabel: string
    assigned: string
    statusLabel: string
    nextAction: string
    unassigned: string
    rowSummary: string
    loading: string
    empty: string
    emptyHint: string
    noAccess: string
    noManage: string
    back: string
    createdAt: string
    serviceabilityLabel: string
    blocksRoomUse: string
    blocksYes: string
    blocksNo: string
    outageLabel: string
    managerActions: string
    assign: string
    reassign: string
    changePriority: string
    changeBlocking: string
    startTitle: string
    startIntro: string
    start: string
    resolveTitle: string
    resolve: string
    resolutionNote: string
    unable: string
    unableNote: string
    resumeTitle: string
    resume: string
    preparationImpact: string
    history: string
    noHistory: string
    status: { Open: string; InProgress: string; UnableToResolve: string; Resolved: string }
    priority: { Normal: string; High: string; Urgent: string }
    needed: { assign: string; start: string; resolve: string; resume: string; none: string }
    serviceability: { Serviceable: string; OutOfOrder: string; OutOfService: string }
    outage: { OutOfOrder: string; OutOfService: string }
    impact: { None: string; RequiresPreparation: string }
    historyEvent: {
      Created: string
      Assigned: string
      Reassigned: string
      PriorityChanged: string
      BlockingChanged: string
      Started: string
      UnableToResolve: string
      Resumed: string
      Resolved: string
    }
    errors: {
      issueNotFound: string
      roomNotFound: string
      categoryNotFound: string
      employeeNotFound: string
      invalidTransition: string
      assignmentRequired: string
      invalidPriority: string
      invalidBlocking: string
      noteRequired: string
      invalidPreparationImpact: string
      staleIssue: string
      roomInactive: string
      preparationFailed: string
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
    technicalService: 'Technical Service',
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
    distribution: 'Room readiness mix',
    roomsInSnapshot: '{{value}} rooms',
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
    applicableDepartments: 'Departments where this position can be used',
    rename: 'Rename',
    save: 'Save',
    cancel: 'Cancel',
    search: 'Search',
    searchPlaceholder: 'Name or personnel number',
    allDepartments: 'All departments',
    loading: 'Loading staff…',
    emptyActive: 'No staff members yet.',
    emptyActiveHint: 'When someone joins the hotel, they will appear here.',
    emptyScheduled: 'No scheduled starts.',
    emptyScheduledHint: 'Future start dates will show in this view.',
    emptyFormer: 'No former staff yet.',
    emptyFormerHint: 'People who have left remain visible here.',
    emptySearch: 'No staff match this search.',
    emptySearchHint: 'Try a different name, number, or department.',
    emptyDepartments: 'No departments yet.',
    emptyDepartmentsHint: 'Add the hotel structure before hiring.',
    emptyPositions: 'No positions yet.',
    emptyPositionsHint: 'Add a job title before hiring.',
    hireNeedsStructure: 'Add at least one active department and one active position before hiring.',
    personalSection: 'Staff details',
    startSection: 'Work details',
    placementSection: 'Assignment',
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
    selectDepartmentFirst: 'Select a department first',
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
      positionNotAvailable: 'The selected position cannot be used in this department.',
      generic: 'The workforce request could not be completed.',
    },
  },
  personnel: {
    newPersonnel: 'New personnel',
    cardTitle: 'Personnel card',
    cardTitleCreate: 'New personnel',
    close: 'Close',
    closeCard: 'Close personnel card',
    cancel: 'Cancel',
    save: 'Save',
    saving: 'Saving…',
    createSuccess: 'Personnel record created.',
    saveSuccess: 'Personnel record saved.',
    photo: 'Photo',
    photoHint: 'JPEG, PNG or WebP. Maximum 2 MB.',
    uploadPhoto: 'Add photo',
    replacePhoto: 'Replace photo',
    removePhoto: 'Remove photo',
    organization: 'Organization',
    property: 'Property',
    notes: 'Notes',
    columns: 'Columns',
    columnPicker: 'Visible columns',
    columnFixed: 'Fixed columns',
    columnOptional: 'Optional columns',
    allPositions: 'All positions',
    allStatuses: 'All statuses',
    startFrom: 'Start from',
    startTo: 'Start to',
    filters: 'Filters',
    tabGeneral: 'General',
    tabIdentity: 'Identity & contact',
    tabWork: 'Work / organization',
    tabHistory: 'History',
    sectionIdentity: 'Identity',
    sectionContact: 'Contact',
    sectionAddress: 'Address',
    sectionEmergency: 'Emergency contacts',
    identityScheme: 'Identity scheme',
    identityNumber: 'Identity number',
    nationality: 'Nationality',
    gender: 'Gender',
    birthDate: 'Date of birth',
    birthPlace: 'Place of birth',
    maritalStatus: 'Marital status',
    bloodType: 'Blood type',
    educationLevel: 'Education level',
    mobilePhone: 'Mobile phone',
    homePhone: 'Home phone',
    email: 'Email',
    residenceAddress: 'Residence address',
    city: 'City',
    district: 'District',
    notificationAddress: 'Notification / stay address',
    emergencyName: 'Name',
    emergencyRelationship: 'Relationship',
    emergencyPhone: 'Phone',
    emergencyPrimary: 'Primary contact',
    addEmergency: 'Add emergency contact',
    removeEmergency: 'Remove',
    noEmergency: 'No emergency contacts yet.',
    placeholders: {
      givenName: 'Enter given name',
      familyName: 'Enter family name',
      email: 'Enter email address',
      birthPlace: 'Enter place of birth',
      educationLevel: 'Select education level',
      bloodType: 'Select blood type',
      identityScheme: 'Select identity type',
      nationality: 'Enter nationality',
      maritalStatus: 'Select marital status',
      gender: 'Select gender',
      identityNumber: 'Enter identity number',
      homePhone: 'Enter home phone',
      city: 'Enter city',
      district: 'Enter district',
      residenceAddress: 'Enter residence address',
      notificationAddress: 'Enter notification address',
      notes: 'Enter notes',
      emergencyName: 'Enter name',
      emergencyRelationship: 'Enter relationship',
      emergencyPhone: 'Enter phone',
    },
    schemeNone: 'Not specified',
    schemeTckn: 'TCKN',
    schemeYkn: 'YKN',
    schemePassport: 'Passport',
    schemeOther: 'Other',
    unspecified: 'Not specified',
    genderFemale: 'Female',
    genderMale: 'Male',
    maritalSingle: 'Single',
    maritalMarried: 'Married',
    maritalDivorced: 'Divorced',
    maritalWidowed: 'Widowed',
    educationPrimary: 'Primary',
    educationSecondary: 'Secondary',
    educationHighSchool: 'High school',
    educationAssociate: 'Associate degree',
    educationBachelor: 'Bachelor’s degree',
    educationMaster: 'Master’s degree',
    educationDoctorate: 'Doctorate',
    dirtyTitle: 'Unsaved changes',
    dirtyBody:
      'Your changes have not been saved.\nIf you leave without saving, these changes will be lost.',
    dirtyContinue: 'Continue editing',
    dirtyDiscard: 'Leave without saving',
    personnelNumberAuto: 'Generated automatically when saved',
    personnelNumberReadOnly: 'Personnel number cannot be changed here.',
    noPositionsForDepartment: 'No positions are available for this department.',
    createNeedsStructure: 'Add at least one active department and one active position before creating personnel.',
    historyEmptyCreate: 'History appears after the person is hired.',
    employmentPeriod: 'Employment',
    assignmentPeriod: 'Assignment',
    transferAction: 'Change of duties',
    noHrAccess: 'You do not have access to personnel administration.',
    sensitiveHidden: 'Restricted fields are hidden for this account.',
    errors: {
      nationalIdentityInUse: 'This national identity already belongs to someone in the organization.',
      invalidHrProfile: 'Check the personnel profile fields and try again.',
      invalidEmergencyContact: 'Each emergency contact needs a name and phone. Only one can be primary.',
      invalidPhoto: 'Use a JPEG, PNG or WebP image up to 2 MB.',
      sensitiveWriteForbidden: 'This account cannot change restricted personnel fields.',
      generic: 'The personnel request could not be completed.',
      fixFields: 'Some fields could not be saved. Check the highlighted fields.',
    },
    validation: {
      tcknLength: 'TCKN must be 11 digits.',
      tcknInvalid: 'Check the TCKN format.',
      yknFormat: 'Check the foreign identity number format.',
      passportFormat: 'Check the passport number.',
      identitySchemeRequired: 'Select an identity scheme when a number is entered.',
      identityTooLong: 'The identity number is too long.',
      identityInvalid: 'Check the identity number.',
      phoneInvalid: 'Check the mobile phone format.',
      homePhoneInvalid: 'Check the home phone format.',
      emergencyPhoneInvalid: 'Check the emergency contact phone number.',
      phoneRequired: 'A phone number is required.',
      mobilePhoneLength: 'Mobile phone must be 10 digits.',
      emailInvalid: 'Check the email address format.',
      emailTooLong: 'The email address is too long.',
      birthDateInvalid: 'Birth date is outside a reasonable range.',
      textTooLong: 'This field is too long.',
      givenNameRequired: 'Given name is required.',
      givenNameTooLong: 'Given name is too long.',
      familyNameRequired: 'Family name is required.',
      familyNameTooLong: 'Family name is too long.',
      personnelNumberRequired: 'Personnel number is required.',
      personnelNumberTooLong: 'Personnel number is too long.',
      emergencyNameRequired: 'Emergency contact name is required.',
      emergencyNameTooLong: 'Emergency contact name is too long.',
      emergencyPrimaryMultiple: 'Only one emergency contact can be primary.',
      departmentRequired: 'Select a department.',
      positionRequired: 'Select a position.',
      startDateRequired: 'Employment start date is required.',
      positionNotAvailable: 'The selected position cannot be used in this department.',
    },
  },
  roomOperations: {
    title: 'Room Operations',
    intro: 'Which rooms need readiness attention now.',
    room: 'Room',
    readinessLabel: 'Readiness',
    technicalCondition: 'Technical condition',
    activeTechnicalIssue: 'Active technical issue',
    viewTechnicalIssue: 'View technical service record',
    assignedEmployee: 'Assigned',
    priorityLabel: 'Priority',
    workState: 'Work status',
    actionNeeded: 'Next action',
    unassigned: 'Unassigned',
    noPriority: '—',
    noWork: 'No current work',
    rowSummary:
      'Room {{number}}. Readiness {{readiness}}. Technical condition {{technical}}. {{person}}. Priority {{priority}}. {{work}}. {{action}}.',
    rejectionHint: 'Required when rejecting the room.',
    loading: 'Loading rooms…',
    empty: 'No rooms are available yet.',
    emptyHint: 'Rooms appear here once the property has been set up.',
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
    serviceability: {
      Serviceable: 'Available',
      OutOfOrder: 'Unavailable',
      OutOfService: 'Out of service',
    },
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
  maintenance: {
    title: 'Technical Service',
    intro: 'Which technical work needs attention now.',
    create: 'New issue',
    createIntro: 'Record a room fault, its priority, and the person responsible.',
    createSubmit: 'Create issue',
    detailTitle: 'Issue',
    detailIntro: 'Current state, next action, and history.',
    room: 'Room',
    issue: 'Issue',
    category: 'Category',
    priorityLabel: 'Priority',
    assigned: 'Assigned technician',
    statusLabel: 'Status',
    nextAction: 'Next action',
    unassigned: 'Unassigned',
    rowSummary:
      'Room {{room}}. {{issue}}. Priority {{priority}}. {{person}}. {{status}}. {{action}}.',
    loading: 'Loading technical work…',
    empty: 'No technical work yet.',
    emptyHint: 'New issues appear here when they are recorded.',
    noAccess: 'You do not have access to technical service.',
    noManage: 'You cannot create or assign technical work.',
    back: 'Back to technical service',
    createdAt: 'Created',
    serviceabilityLabel: 'Technical condition',
    blocksRoomUse: 'Blocks room use',
    blocksYes: 'Yes — the room cannot be used',
    blocksNo: 'No — the room can still be used',
    outageLabel: 'Expected duration',
    managerActions: 'Assignment and classification',
    assign: 'Assign',
    reassign: 'Reassign',
    changePriority: 'Change priority',
    changeBlocking: 'Update blocking',
    startTitle: 'Start work',
    startIntro: 'Work can start after a technician is assigned.',
    start: 'Start',
    resolveTitle: 'Complete work',
    resolve: 'Resolved',
    resolutionNote: 'How it was resolved',
    unable: 'Unable to resolve',
    unableNote: 'Why it cannot be resolved now',
    resumeTitle: 'Continue work',
    resume: 'Resume',
    preparationImpact: 'Did the repair affect room preparation?',
    history: 'History',
    noHistory: 'No history yet.',
    status: {
      Open: 'Open',
      InProgress: 'In progress',
      UnableToResolve: 'Unable to resolve',
      Resolved: 'Resolved',
    },
    priority: { Normal: 'Normal', High: 'High', Urgent: 'Urgent' },
    needed: {
      assign: 'Assign technician',
      start: 'Start work',
      resolve: 'Resolve',
      resume: 'Resume work',
      none: 'No action',
    },
    serviceability: {
      Serviceable: 'Technically usable',
      OutOfOrder: 'Out of order',
      OutOfService: 'Out of service',
    },
    outage: {
      OutOfOrder: 'Same-day repair expected',
      OutOfService: 'Not same-day',
    },
    impact: {
      None: 'Preparation was not affected',
      RequiresPreparation: 'Room needs preparation again',
    },
    historyEvent: {
      Created: 'Created',
      Assigned: 'Assigned',
      Reassigned: 'Reassigned',
      PriorityChanged: 'Priority changed',
      BlockingChanged: 'Blocking changed',
      Started: 'Started',
      UnableToResolve: 'Unable to resolve',
      Resumed: 'Resumed',
      Resolved: 'Resolved',
    },
    errors: {
      issueNotFound: 'The technical issue was not found.',
      roomNotFound: 'The room was not found.',
      categoryNotFound: 'The category was not found.',
      employeeNotFound: 'The employee was not found or is not currently employed.',
      invalidTransition: 'That step is not allowed for this issue now.',
      assignmentRequired: 'Assign a technician before starting work.',
      invalidPriority: 'Choose Normal, High, or Urgent.',
      invalidBlocking: 'A blocking issue needs a same-day or not-same-day classification.',
      noteRequired: 'A note is required.',
      invalidPreparationImpact: 'Say whether the repair affected room preparation.',
      staleIssue: 'This issue was changed by someone else. Reload and try again.',
      roomInactive: 'An inactive room cannot receive a technical issue.',
      preparationFailed: 'The issue was recorded, but room preparation could not be requested.',
      generic: 'The technical service request could not be completed.',
    },
  },
}
