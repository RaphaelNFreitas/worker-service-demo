using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerServiceDemo.Core;
using WorkerServiceDemo.Database;
using WorkerServiceDemo.Settings;

namespace WorkerServiceDemo
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerContext _context;
        private readonly AppSetting _appSetting;

        private FileSystemWatcher _fileSystemWatcher;
        public Worker(ILogger<Worker> logger,
            WorkerContext context,
            AppSetting appSetting)
        {
            _logger = logger;
            _context = context;
            _appSetting = appSetting;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[+] Inicializando Execucao [+]");

            Task.Factory.StartNew(() =>
             {
                 _fileSystemWatcher = new FileSystemWatcher
                 {
                     Path = _appSetting.CaminhoImagens,
                     IncludeSubdirectories = true,
                     NotifyFilter = NotifyFilters.LastWrite
                                    | NotifyFilters.FileName
                 };

                 _logger.LogInformation($"[+] Escutando caminho - {_appSetting.CaminhoImagens} [+]");

                 _fileSystemWatcher.Created += FileSystemWatcherOnCreated;
                 _fileSystemWatcher.Error += FileSystemWatcherOnError;
                 _fileSystemWatcher.EnableRaisingEvents = true;
                 _fileSystemWatcher.WaitForChanged(WatcherChangeTypes.All);

             }, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"[+] Total de Images: {_context.Imagens.Count()} [+]");
                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

            return base.StartAsync(stoppingToken);
        }

        private void FileSystemWatcherOnError(object sender, ErrorEventArgs e)
        {
            _logger.LogCritical("[+] ERRO: {message} [+]", e.GetException().Message);
        }

        private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(e.Name))
                        return;

                    _context.Imagens.Add(new Imagem
                    {
                        Caminho = e.FullPath,
                        Nome = e.Name
                    });
                    _context.SaveChanges();
                    _logger.LogInformation("[+] Nova Imagem Recebida: {image} [+]", e.Name);

                }
                catch (Exception exception)
                {
                    _logger.LogCritical("[+] Erro: {mensagem} [+]", exception.Message);
                }
            });
        }

    }
}
