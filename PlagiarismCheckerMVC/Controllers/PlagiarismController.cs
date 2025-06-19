using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlagiarismCheckerMVC.Models;
using PlagiarismCheckerMVC.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PlagiarismCheckerMVC.Controllers
{
    [ApiController]
    [Route("plag-api")]
    [Authorize]
    public class PlagiarismController : ControllerBase
    {
        private readonly IPlagiarismService _plagiarismService;
        private readonly IDocumentService _documentService;

        public PlagiarismController(
            IPlagiarismService plagiarismService,
            IDocumentService documentService)
        {
            _plagiarismService = plagiarismService;
            _documentService = documentService;
        }

        [HttpPost("check-uploaded")]
        public async Task<ActionResult<DocCheckReport>> CheckPlagiarism(Guid documentId, string searchEngine)
        {
            try
            {
                var userId = GetUserId();
                // Проверяем, принадлежит ли документ пользователю
                var document = await _documentService.GetByIdAsync(documentId);
                if (document == null || document.UserId != userId)
                {
                    return NotFound(new { message = "Документ не найден или не принадлежит вам" });
                }

                if (!Enum.TryParse<SearchEngineType>(searchEngine, true, out var searchEngineType))
                {
                    searchEngineType = SearchEngineType.Google; // По умолчанию
                }

                var result = await _plagiarismService.CheckDocumentAsync(documentId, searchEngineType);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Произошла ошибка при проверке на плагиат", details = ex.Message });
            }
        }

        [HttpPost("check-new")]
        public ActionResult<DocCheckReport> CheckPlagiarism([FromForm] IFormFile docFile, [FromForm] string searchEngine)
        {
            try
            {
                if (docFile == null || docFile.Length == 0)
                {
                    return BadRequest(new { message = "Файл не был загружен или пуст" });
                }

                if (!Path.GetExtension(docFile.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { message = "Допускаются только файлы в формате .docx" });
                }

                if (!Enum.TryParse<SearchEngineType>(searchEngine, true, out var searchEngineType))
                {
                    searchEngineType = SearchEngineType.Google; // По умолчанию Google
                }

                using var fileStream = new MemoryStream();
                docFile.CopyTo(fileStream);
                fileStream.Position = 0;

                int characterCount = DocumentService.CountCharacters(fileStream);
                if (characterCount < 1000 || characterCount > 40000)
                {
                    // return BadRequest(new { message = $"Текст не соответствует требованиям. Минимум 1000 символов, максимум 40000 символов. Найдено: {characterCount} символов" });
                }

                fileStream.Position = 0;
                var checkReport = _plagiarismService.CheckLocalDocumentAsync(fileStream, searchEngineType);
                checkReport.DocumentName = docFile.FileName;

                return Ok(checkReport);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Произошла ошибка при проверке на плагиат", details = ex.Message });
            }
        }

        [Obsolete]
        private decimal CalculateOriginalityPercentage(List<QuerySearchResult> results)
        {
            if (results == null || !results.Any())
                return 100m;

            int totalPlagiarismCount = results.Count;
            int highPlagiarismCount = results.Count(r => r.SimilarityScore > 0.7);
            int mediumPlagiarismCount = results.Count(r => r.SimilarityScore > 0.4 && r.SimilarityScore <= 0.7);

            decimal originalityScore = 100m -
                                      (highPlagiarismCount * 5m) -
                                      (mediumPlagiarismCount * 2m) -
                                      (totalPlagiarismCount * 0.5m);

            return Math.Max(0m, Math.Min(100m, originalityScore));
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