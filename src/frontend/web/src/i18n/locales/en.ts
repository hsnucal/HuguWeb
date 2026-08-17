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
}
