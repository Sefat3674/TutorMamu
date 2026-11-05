
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TutorMamu.Application.DTOs.Tutors;

public class UploadTutorDocumentRequest
{
    [Required]
    public string DocumentType { get; set; } = string.Empty;

    public string? DocumentSide { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
