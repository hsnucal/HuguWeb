namespace HuGuWeb.RoomOperations.Domain;

public enum ReadinessChangeCause
{
    Seeded = 0,
    NeedsCleaning = 1,
    CleaningCompleted = 2,
    InspectionAccepted = 3,
    InspectionRejected = 4
}
