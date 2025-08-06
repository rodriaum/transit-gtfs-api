namespace Tranzor.Enums;

public enum PickupType
{
    /// <summary>
    /// Normal stop, allowed to pick up passengers here
    /// </summary>
    Regular = 0,

    /// <summary>
    /// Does not allow boarding (or disembarking)
    /// summary>
    NoPickup = 1,

    /// <summary>
    /// On request only (on-demand pickup/discharge)
    /// summary>
    MustPhoneAgency = 2,

    /// <summary>
    /// Necessary coordination with the driver or company
    /// summary>
    MustCoordinateWithDriver = 3
}