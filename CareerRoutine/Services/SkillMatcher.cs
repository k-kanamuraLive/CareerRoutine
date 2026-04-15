using CareerRoutine.Models;

namespace CareerRoutine.Services
{
    public class SkillMatcher
    {
        public Job? PickOne(List<Job> jobs)
        {
            if (jobs == null || !jobs.Any())
                return null;

            // スキルにマッチするものだけ抽出
            var matched = jobs
                .Where(j => IsSkillMatch(j))
                .OrderByDescending(j => j.InternalDate)
                .FirstOrDefault();

            return matched; // マッチがなければ null
        }

        private bool IsSkillMatch(Job job)
        {
            string fullText = job.GetFullText();

            return Contains(fullText, "C++") ||
                   Contains(fullText, "VisualStudio") ||
                   Contains(fullText, "C#") ||
                   Contains(fullText, "パッケージ") ||
                   Contains(fullText, "Windows");
        }

        private bool Contains(string text, string keyword)
        {
            return text.Contains(keyword,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
