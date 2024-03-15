namespace Service.DInspect.Models.Enum
{
    public static class EnumStatus
    {
        #region E-Form Status

        public static string EFormOpen { get { return "Open"; } }
        public static string EFormRevise { get { return "Revised"; } }
        public static string EFormOnProgress { get { return "On Progress"; } }
        public static string EFormSubmited { get { return "Submited"; } }
        public static string EFormApprovedSPV { get { return "Approved (SPV)"; } }
        public static string EFormFinalReview { get { return "Final Review"; } }
        public static string EFormClosed { get { return "Close"; } }

        #endregion

        #region E-Form Defect Status

        public static string DefectApprovedSPV { get { return "Approved (SPV)"; } }
        public static string DefectApprovedPLN { get { return "Approved (PLN)"; } }
        public static string DefectCompleted { get { return "Completed"; } }

        #endregion

        #region Task Defect Status

        public static string DefectSubmit { get { return "Submit"; } }
        public static string DefectSubmited { get { return "Submited"; } }
        public static string DefectAcknowledge { get { return "Acknowledge"; } }
        public static string DefectApproved { get { return "Approved"; } }
        public static string DefectNotApproved { get { return "Not Approved"; } }
        public static string DefectNotAcknowledge { get { return "Not Acknowledge"; } }
        public static string DefectDecline { get { return "Decline"; } }
        public static string DefectConfirm { get { return "Confirmed"; } }


        #endregion

        #region InterventionStatus

        public static string InterventionDeclined { get { return "Declined"; } }
        public static string InterventionAccepted { get { return "Accepted"; } }

        #endregion

        #region Interim Engin Service Status
        public static string IEngineOpen { get { return "Open"; } }
        public static string IEngineOnProgress { get { return "On Progress"; } }
        public static string IEngineSubmited { get { return "Submited"; } }
        public static string IEngineApprovedSPV { get { return "Approved (SPV)"; } }
        public static string IEngineClosed { get { return "Close"; } }
        #endregion
    }
}
