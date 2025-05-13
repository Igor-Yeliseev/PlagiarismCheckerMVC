using System.Collections.Generic;
using System.Threading.Tasks;
using PlagiarismCheckerMVC.Models;

namespace PlagiarismCheckerMVC.Services
{
    public interface ISearchService
    {
        Task<List<SearchItem>> SearchGoogleAsync(string query);
        Task<List<SearchItem>> SearchYandexAsync(string query);
    }
} 