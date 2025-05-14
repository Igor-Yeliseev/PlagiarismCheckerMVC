using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlagiarismCheckerMVC.Models;
using PlagiarismCheckerMVC.Services;

namespace PlagiarismCheckerMVC.Controllers
{
    [ApiController]
    [Route("plag-api")]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost("add-doc")]
        public async Task<ActionResult<Document>> UploadDocument([FromForm] IFormFile file)
        {
            try
            {
                var userId = GetUserId();

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Файл не найден или пустой" });
                }
                if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Разрешены только файлы .docx" });
                }

                var document = await _documentService.UploadAsync(file, userId);

                return Ok(document);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Произошла ошибка при загрузке документа", details = ex.Message });
            }
        }

        [HttpGet("documents")]
        public async Task<ActionResult<IEnumerable<DocumentDTO>>> GetUserDocuments()
        {
            try
            {
                var userId = GetUserId();
                var documents = await _documentService.GetUserDocumentsWithOriginalityAsync(userId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Произошла ошибка при получении документов пользователя", details = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<ActionResult> DeleteDocument(Guid id)
        {
            try
            {
                var userId = GetUserId();
                await _documentService.DeleteAsync(id, userId);
                return Ok(new { message = "Документ успешно удален" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Произошла ошибка при удалении документа", details = ex.Message });
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("Невозможно определить идентификатор пользователя");
            }
            return userId;
        }
    }
}