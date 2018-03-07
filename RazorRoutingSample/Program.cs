using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace ConsoleApp773
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://*:5000")
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMvc();
                    services.Configure<RazorViewEngineOptions>(opts =>
                        opts.FileProviders.Add(new MemoryFileProvider()));

                    services.Configure((RazorViewEngineOptions options) =>
                    {
                        var previous = options.CompilationCallback;
                        options.CompilationCallback = context =>
                        {
                            previous?.Invoke(context);

                            var assembly = typeof(Program).GetTypeInfo().Assembly;
                            var assemblies = assembly.GetReferencedAssemblies().Select(x => MetadataReference.CreateFromFile(Assembly.Load(x).Location))
                                .ToList();
                            assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("netstandard")).Location));
                            assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Linq.Expressions")).Location));

                            assemblies.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("System.Private.Corelib")).Location));

                            context.Compilation = context.Compilation.AddReferences(assemblies);
                        };
                    });

                    services.AddLogging(builder => builder.AddDebug().AddConsole());
                })
                .Configure(app =>
                    app.UseMvc(routes => routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}")))
                .Build()
                .Run();
        }
    }

    public class Home : Controller
    {
        public IActionResult Index() => View("Index");

        public IActionResult Other(Model model, int id) => Json(new {model, id});
    }

    public class Model
    {
        public string Name { get; set; }
    }

    public abstract class MemoryFileInfo : IFileInfo
    {
        protected MemoryFileInfo(string name) => Name = name;
        protected abstract string Content { get; }
        public abstract bool Exists { get; }

        public long Length => Content.Length;

        public string PhysicalPath => null;

        public string Name { get; }

        public DateTimeOffset LastModified => DateTimeOffset.Now;

        public bool IsDirectory => false;

        public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(Content));
    }

    public class IndexFileInfo : MemoryFileInfo
    {
        public IndexFileInfo(string name) : base(name)
        {
        }

        protected override string Content => @"
            <!DOCTYPE html>
            <html>
                <head>
                    <meta charset=""utf-8"" />
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
                </head>
                <body>
                    @using (Html.BeginForm(""Other"", ""Home"", new { Id = 5}))
                    {
                        <p>Enter your name :</p>
                        <input type=""text"" name=""Name"" />
                        <input type=""submit"" value=""Submit"" />
                    }
                    
                </body>
            </html>
        ";

        public override bool Exists => Name.Equals("/Views/Home/Index.cshtml");
    }

    public class MemoryFileProvider : IFileProvider
    {
        public IFileInfo GetFileInfo(string subpath)
        {
            var result = new IndexFileInfo(subpath);
            return result.Exists
                ? (IFileInfo) result
                : new NotFoundFileInfo(subpath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath) => new DirectoryContents();

        public IChangeToken Watch(string filter) => new ChangeToken();
    }

    public class DirectoryContents : IDirectoryContents
    {
        public IEnumerator<IFileInfo> GetEnumerator() => throw new NotImplementedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Exists => false;
    }

    public class ChangeToken : IChangeToken
    {
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => EmptyDisposable.Instance;

        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;

        internal class EmptyDisposable : IDisposable
        {
            private EmptyDisposable()
            {
            }

            public static EmptyDisposable Instance { get; } = new EmptyDisposable();

            public void Dispose()
            {
            }
        }
    }
}