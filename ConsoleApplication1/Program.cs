using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;



namespace http_filetransfer
{

    class Program
    {

        public static string ServerDirectory = "C:/Server";
        public static int BufferSize = 20000;
        static void Main(string[] args)
        {
            var httplistener = new HttpListener();
            try
            {
                httplistener.Prefixes.Add("http://*:80/");
                httplistener.Start();

                while (true)
                {
                    IHttpCommand command;
                    HttpListenerContext httplistenercontext = httplistener.GetContext();
                    string commandname = httplistenercontext.Request.HttpMethod;
                    if (commandname == "PUT")
                         command = new PutCommand();
                    else
                        if (commandname == "DELETE")
                             command = new DeleteCommand();
                        else
                            if (commandname == "HEAD")
                                command = new HeadCommand();
                            else
                                if (commandname == "GET")
                                command = new GetCommand();
                            else
                        {
                            httplistenercontext.Response.StatusCode = 501;
                            httplistenercontext.Response.OutputStream.Close();
                            continue;
                        }
                    HttpListenerResponse response = httplistenercontext.Response;

                    command.Process(httplistenercontext.Request, ref response);

                }
            }
            catch 
            {
                Console.WriteLine("Smthng broken."); 
            }
            finally { httplistener.Close(); }
            Console.ReadLine();
        }
    

    public interface IHttpCommand
    {
        void Process(HttpListenerRequest request, ref HttpListenerResponse response);
    }

    public class PutCommand : IHttpCommand
    {

        public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
        {
            string fullPath = ServerDirectory + request.RawUrl;

            try
            {
                    var dir = Path.GetDirectoryName(fullPath);

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (var newFile = new FileStream(fullPath, FileMode.Create))
                    {
                        request.InputStream.CopyTo(newFile, BufferSize);
                    }

                    return;                
                
            }
            catch (FileNotFoundException)
            {
                response.StatusCode = 404;
            }
            catch (DirectoryNotFoundException)
            {
                response.StatusCode = 404;
            }
            catch
            {
                response.StatusCode = 400;
            }
            finally { response.OutputStream.Close(); }
        }
    }


        public class DeleteCommand : IHttpCommand
        {
            
            public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
            {
                try
                {
                    if (Directory.Exists(ServerDirectory + request.RawUrl))
                        Directory.Delete(ServerDirectory + request.RawUrl);
                    else if (File.Exists(ServerDirectory + request.RawUrl))
                        File.Delete(ServerDirectory + request.RawUrl);
                }
                catch (FileNotFoundException)
                {
                    response.StatusCode = 404;
                }
                catch (DirectoryNotFoundException)
                {
                    response.StatusCode = 404;
                }
                catch (Exception)
                {
                    response.StatusCode = 400;
                }
                finally { response.OutputStream.Close(); }
            }
        }



        public class HeadCommand : IHttpCommand
        {

            public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
            {
                string fullPath = ServerDirectory + request.RawUrl;

                try
                {
                    var fileInfo = new FileInfo(fullPath);
                    response.Headers.Add("Name", fileInfo.Name);
                    response.Headers.Add("FileLength", fileInfo.Length.ToString());
                    response.Headers.Add("LastWriteTime", fileInfo.LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
                }
                catch (FileNotFoundException)
                {
                    response.StatusCode = 404;
                }
                catch (DirectoryNotFoundException)
                {
                    response.StatusCode = 404;
                }
                catch (Exception)
                {
                    response.StatusCode = 400;
                }
                finally { response.OutputStream.Close(); }
            }
        }

        public class GetCommand : IHttpCommand
    {

        public void Process(HttpListenerRequest request, ref HttpListenerResponse response)
        {
            Stream output = response.OutputStream;

            var writer = new StreamWriter(output);

            string fullPath = ServerDirectory + request.RawUrl;

            try
            {
                if (File.Exists(fullPath))
                {
                    Stream file = new FileStream(fullPath, FileMode.Open);
                    file.CopyTo(output, BufferSize);  
                }
                else
                {
                    var directoryListing = Directory.EnumerateFiles(fullPath);

                    foreach (var entry in directoryListing)
                    {
                        writer.Write(JsonConvert.SerializeObject(entry, new JsonSerializerSettings() { Formatting = Formatting.Indented }));
                    }
                    writer.Flush();
                }
            }
            catch (FileNotFoundException)
            {
                response.StatusCode = 404;
            }
            catch (DirectoryNotFoundException)
            {
                response.StatusCode = 404;
            }
            catch
            {
                response.StatusCode = 400;
            }
            finally 
            { 
                output.Close();
                writer.Dispose(); 
            }
        }
    }



}
    
}
