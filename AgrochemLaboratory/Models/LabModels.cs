using System;
using System.Collections.Generic;

namespace AgrochemLaboratory.Models
{
    public class ApiResponse<T>
    {
        public bool success { get; set; }
        public string message { get; set; }
        public T data { get; set; }
        public int count { get; set; }
    }

    public class RawMaterialBatchModel
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Supplier { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Quantity { get; set; }
        public string CurrentStatus { get; set; }
        public bool HasActiveTest { get; set; }
        public int? ActiveTestId { get; set; }
    }

    public class ProductionBatchModel
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTime? Date { get; set; }
        public decimal? Quantity { get; set; }
        public string CurrentStatus { get; set; }
        public bool HasActiveTest { get; set; }
        public int? ActiveTestId { get; set; }

    }

    public class LabTestModel
    {
        public int Id { get; set; }
        public string ObjectType { get; set; }
        public int ObjectId { get; set; }
        public string ObjectName { get; set; }
        public string ObjectNumber { get; set; }
        public string TestType { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }
        public string Comment { get; set; }
        public string CreatedAt { get; set; }
        public string AuthorName { get; set; }
        public List<LabTestParameterModel> Parameters { get; set; }
        public bool CanMakeDecision { get; set; }
    }

    public class LabTestParameterModel
    {
        public int Id { get; set; }
        public string ParameterName { get; set; }
        public decimal? NormMin { get; set; }
        public decimal? NormMax { get; set; }
        public decimal? ActualValue { get; set; }
        public bool? IsPassed { get; set; }
    }

    public class CreateLabTestDto
    {
        public string ObjectType { get; set; }
        public int ObjectId { get; set; }
        public string TestType { get; set; }
        public int CreatedBy { get; set; }
        public string Comment { get; set; }
    }

    public class LabTestParameterDto
    {
        public int? Id { get; set; }
        public string ParameterName { get; set; }
        public decimal? NormMin { get; set; }
        public decimal? NormMax { get; set; }
        public decimal? ActualValue { get; set; }
        public bool? IsPassed { get; set; }
    }

    public class EnterLabResultDto
    {
        public int TestId { get; set; }
        public int UserId { get; set; }
        public List<LabTestParameterDto> Parameters { get; set; }
        public string Comment { get; set; }
    }

    public class LabDecisionDto
    {
        public int TestId { get; set; }
        public int UserId { get; set; }
        public string Result { get; set; }
        public string Comment { get; set; }
    }

    public class DeviationModel
    {
        public int Id { get; set; }
        public string BatchNumber { get; set; }
        public string Parameter { get; set; }
        public string PlannedValue { get; set; }
        public string ActualValue { get; set; }
        public string Severity { get; set; }
        public string Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class RecipeModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public int Version { get; set; }
        public string Status { get; set; }
    }

    
}