namespace TransitGtfsApi.Enums
{
    public enum RouteType
    {
        #region Core Types (0–7)
        Tram = 0,
        Subway = 1,
        Rail = 2,
        Bus = 3,
        Ferry = 4,
        CableCar = 5,
        Gondola = 6,
        Funicular = 7,
        #endregion

        #region Extended Rail Types (100–117)
        HighSpeedRail = 100,
        LongDistanceRail = 101,
        InterRegionalRail = 102,
        CarTransportRail = 103,
        SleeperRail = 104,
        RegionalRail = 105,
        TouristRail = 106,
        RailShuttle = 107,
        SuburbanRail = 108,
        ReplacementRail = 109,
        SpecialRail = 110,
        LorryRail = 111,
        AllRailServices = 112,
        CrossCountryRail = 113,
        IntercityRail = 114,
        InternationalRail = 115,
        LocalPassengerRail = 116,
        NightRail = 117,
        #endregion

        #region Metro / Light Rail Types (200–205)
        Metro = 200,
        Underground = 201,
        UrbanRail = 202,
        AllUrbanRail = 203,
        Monorail = 204,
        LightRail = 205,
        #endregion

        #region Bus Subtypes (300–312)
        LocalBus = 300,
        NightBus = 301,
        ExpressBus = 302,
        RegionalBus = 303,
        SpecialNeedsBus = 304,
        MobilityBus = 305,
        MobilityVan = 306,
        TouristBus = 307,
        SchoolBus = 308,
        RailReplacementBus = 309,
        DemandResponsiveBus = 310,
        CommunityBus = 311,
        AllBusServices = 312,
        #endregion

        #region Trolleybus (400)
        Trolleybus = 400,
        #endregion

        #region Tram / Streetcar Types (900–902)
        LocalTram = 900,
        HistoricTram = 901,
        TouristTram = 902,
        #endregion

        #region Water Transport (1000–1006)
        LocalFerry = 1000,
        LongDistanceFerry = 1001,
        InternationalFerry = 1002,
        Cruise = 1003,
        HighSpeedBoat = 1004,
        Hovercraft = 1005,
        WaterTaxi = 1006,
        #endregion

        #region Air Transport (1100–1104)
        InternationalAir = 1100,
        DomesticAir = 1101,
        ShuttleAir = 1102,
        RegionalAir = 1103,
        Helicopter = 1104,
        #endregion

        #region Cableway (1300–1303)
        Telecabin = 1300,
        ChairLift = 1301,
        DragLift = 1302,
        Lift = 1303,
        #endregion

        #region Funicular (1400)
        InclinedRail = 1400,
        #endregion

        #region Taxi / On-Demand Transport (1500–1502)
        SharedTaxi = 1500,
        WaterTaxiAlt = 1501,
        BikeTaxi = 1502,
        #endregion

        #region Other (1700)
        Other = 1700
        #endregion
    }
}