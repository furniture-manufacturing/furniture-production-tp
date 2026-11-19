namespace Furniture.Web.Models;

public enum EmployeeRole { Administrator, Measurer, ProductionWorker, Courier, Assembler }

public enum OrderStatus
{
    New, MeasurementAndApproval, InProduction, ReadyForDelivery,
    Delivery, Assembly, AwaitingPayment, Completed, Cancelled
}

public enum StageType { MeasurementAndApproval, Production, Delivery, Assembly }

public enum StageStatus { WaitingPreviousStage, WaitingForAssignee, Assigned, InProgress, Completed, Skipped }

public enum AssignmentStatus { Assigned, Accepted, Completed, Cancelled }

public enum SubOrderReason { Addition, MissingPart, Defect, Rework }
