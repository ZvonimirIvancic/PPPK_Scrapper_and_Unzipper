using System;
using System.Collections.Generic;
using System.Net.Http;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Threading.Tasks;
using System.IO;

class XenaDataDownloader
{
    private static string _desktopPath;
    private static string _downloadFolder;
    private static IWebDriver _driver;

    static async Task Main(string[] args)
    {
        InitializeDownloadFolder();
        InitializeWebDriver();

        try
        {
            await ProcessXenaDataPage("https://xenabrowser.net/datapages/?host=https%3A%2F%2Ftcga.xenahubs.net&removeHub=https%3A%2F%2Fxena.treehouse.gi.ucsc.edu%3A443");
        }
        finally
        {
            Cleanup();
        }
    }

    private static void InitializeDownloadFolder()
    {
        _desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        _downloadFolder = Path.Combine(_desktopPath, $"XenaDownloads");
        Directory.CreateDirectory(_downloadFolder);
        Console.WriteLine($"Download folder created at: {_downloadFolder}");
    }

    private static void InitializeWebDriver()
    {
        _driver = new ChromeDriver();
    }

    private static async Task ProcessXenaDataPage(string url)
    {
        _driver.Navigate().GoToUrl(url);

        WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        wait.Until(drv => drv.FindElement(By.TagName("ul")));

        var cohortLinks = ExtractCohortLinks();
        Console.WriteLine($"Found {cohortLinks.Count} TCGA cohort links.");

        foreach (var cohortLink in cohortLinks)
        {
            await ProcessCohortPage(cohortLink);
        }
    }

    private static List<string> ExtractCohortLinks()
    {
        System.Threading.Thread.Sleep(2000);
        var links = _driver.FindElements(By.XPath("//ul/li/a[@href]"));
        var cohortLinks = new List<string>();

        foreach (var link in links)
        {
            string href = link.GetAttribute("href");
            if (href.Contains("TCGA"))
            {
                cohortLinks.Add(href);
                Console.WriteLine($"Found cohort link: {href}");
            }
        }

        return cohortLinks;
    }

    private static async Task ProcessCohortPage(string cohortUrl)
    {
        Console.WriteLine($"Navigating to: {cohortUrl}");
        _driver.Navigate().GoToUrl(cohortUrl);

        System.Threading.Thread.Sleep(1000);

        try
        {
            var geneExpressionSection = _driver.FindElement(By.XPath("//div[h3[contains(text(), 'gene expression RNAseq')]]"));
            var illuminaLink = FindIlluminaLink(geneExpressionSection);

            if (!string.IsNullOrEmpty(illuminaLink))
            {
                await ProcessIlluminaPage(illuminaLink);
            }
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine("No 'gene expression RNAseq' section found.");
        }
    }

    private static string FindIlluminaLink(IWebElement geneExpressionSection)
    {
        try
        {
            var ulElement = geneExpressionSection.FindElement(By.XPath(".//ul"));
            var illuminaElement = ulElement.FindElement(By.XPath(".//li/a[contains(text(), 'IlluminaHiSeq') and contains(text(), 'pancan')]"));
            string illuminaUrl = illuminaElement?.GetAttribute("href");

            Console.WriteLine($"Found IlluminaHiSeq pancan normalized link: {illuminaUrl}");
            return illuminaUrl;
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine("No IlluminaHiSeq pancan normalized link found.");
            return null;
        }
    }

    private static async Task ProcessIlluminaPage(string illuminaUrl)
    {
        Console.WriteLine($"Navigating to Illumina link: {illuminaUrl}");
        _driver.Navigate().GoToUrl(illuminaUrl);

        System.Threading.Thread.Sleep(1000);

        WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
        wait.Until(drv => drv.FindElement(By.TagName("a")));

        var downloadLink = _driver.FindElement(By.XPath("//span/a[contains(text(), 'download')]"));
        if (downloadLink != null)
        {
            string fileUrl = downloadLink.GetAttribute("href");
            string fileName = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
            string destinationPath = Path.Combine(_downloadFolder, fileName);

            Console.WriteLine($"Downloading {fileName} to {destinationPath}");
            await DownloadFile(fileUrl, destinationPath);
        }
        else
        {
            Console.WriteLine("Download link not found on the page.");
        }
    }

    private static async Task DownloadFile(string fileUrl, string destinationPath)
    {
        using (HttpClient client = new HttpClient())
        {
            var fileData = await client.GetByteArrayAsync(fileUrl);
            await File.WriteAllBytesAsync(destinationPath, fileData);
            Console.WriteLine($"File downloaded to: {destinationPath}");
        }
    }

    private static void Cleanup()
    {
        Console.WriteLine("All downloads completed.");
        _driver.Quit();
    }
}
