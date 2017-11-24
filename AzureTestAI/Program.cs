using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CSHttpClientSample
{

    static class Program
    {
        static void Main()
        {
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine("Escolha o programa: Análise de imagem (1), Emoções (2)");
            Console.WriteLine("--------------------------------------------------------------");

            var typed = Console.ReadLine();

            if (typed == "1")
                Main1();
            else if (typed == "2")
                Main2();
            else
                Main();
        }

        static void Main2()
        {
            try
            {

                Console.WriteLine("-----------------------------------------");
                Console.WriteLine("Endereço da imagem para emoções:");
                Console.WriteLine("-----------------------------------------");

                string imageFilePath = Console.ReadLine();

                if (imageFilePath == "exit")
                    Main();

                MakeRequest(imageFilePath);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Main2();
            }
            Console.ReadLine(); // wait for ENTER to exit program
        }

        static void Main1()
        {
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("Endereço da imagem:");
            Console.WriteLine("-----------------------------------------");

            try
            {
                var urlx = Console.ReadLine();

                if (urlx == "exit")
                    Main();

                var run = new Chamador().Run(urlx);
                Task.WaitAll(run);
                Main1();
            }
            catch (Exception e)
            {
                Console.WriteLine("-> " + e.Message);
                Main1();
            }

        }

        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        static async void MakeRequest(string imageFilePath)
        {
            var client = new HttpClient();

            // Request headers - replace this example key with your valid key.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "SUA_CHAVE_AQUI"); // 

            // NOTE: You must use the same region in your REST call as you used to obtain your subscription keys.
            //   For example, if you obtained your subscription keys from westcentralus, replace "westus" in the 
            //   URI below with "westcentralus".
            string uri = "https://westus.api.cognitive.microsoft.com/emotion/v1.0/recognize?";
            HttpResponseMessage response;
            string responseContent; string caminhoImagem = "";

            using (var client2 = new WebClient())
            {
                client2.DownloadFile(imageFilePath, "imagem.jpg");
            }

            // Request body. Try this sample with a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray("imagem.jpg");

            using (var content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
                responseContent = response.Content.ReadAsStringAsync().Result;
            }

            // A peak at the raw JSON response.
            //Console.WriteLine(responseContent);

            // Processing the JSON into manageable objects.
            JToken rootToken = JArray.Parse(responseContent).First;

            // First token is always the faceRectangle identified by the API.
            JToken faceRectangleToken = rootToken.First;

            // Second token is all emotion scores.
            JToken scoresToken = rootToken.Last;

            // Show all face rectangle dimensions
            JEnumerable<JToken> faceRectangleSizeList = faceRectangleToken.First.Children();
            foreach (var size in faceRectangleSizeList)
            {
                Console.WriteLine(size);
            }

            // Show all scores
            JEnumerable<JToken> scoreList = scoresToken.First.Children();
            foreach (var score in scoreList)
            {
                Console.WriteLine(score);
            }
        }
    }

    public class Chamador
    {
        public async Task Run(string urlx)
        {
            var url = new Uri(urlx);

            await DoWork(url, false);
        }

        protected async Task DoWork(Uri imageUri, bool upload)
        {
            AnalysisResult analysisResult;

            analysisResult = await AnalyzeUrl(imageUri.AbsoluteUri);

            Log("");
            Console.WriteLine("-> RESULTADO");
            LogAnalysisResult(analysisResult);
        }

        protected void LogAnalysisResult(AnalysisResult result)
        {
            if (result == null)
            {
                Log("null");
                return;
            }

            if (result.Metadata != null)
            {
                Log("Formato da imagem : " + result.Metadata.Format);
                Log("Dimensões : " + result.Metadata.Width + " x " + result.Metadata.Height);
            }

            if (result.ImageType != null)
            {
                string clipArtType;
                switch (result.ImageType.ClipArtType)
                {
                    case 0:
                        clipArtType = "0 Non-clipart";
                        break;
                    case 1:
                        clipArtType = "1 ambiguous";
                        break;
                    case 2:
                        clipArtType = "2 normal-clipart";
                        break;
                    case 3:
                        clipArtType = "3 good-clipart";
                        break;
                    default:
                        clipArtType = "Desconhecido";
                        break;
                }
                //Log("Clip Art Type : " + clipArtType);

                //string lineDrawingType;
                //switch (result.ImageType.LineDrawingType)
                //{
                //    case 0:
                //        lineDrawingType = "0 Non-LineDrawing";
                //        break;
                //    case 1:
                //        lineDrawingType = "1 LineDrawing";
                //        break;
                //    default:
                //        lineDrawingType = "Unknown";
                //        break;
                //}
                //Log("Line Drawing Type : " + lineDrawingType);
            }

            if (result.Adult != null)
            {
                Log("Conteúdo adulto : " + result.Adult.IsAdultContent);
                Log("Pontuação adulto : " + result.Adult.AdultScore);
                Log("Conteúdo picante : " + result.Adult.IsRacyContent);
                Log("Pontuação picante : " + result.Adult.RacyScore);
            }

            if (result.Categories != null && result.Categories.Length > 0)
            {
                Log("Categorias : ");
                foreach (var category in result.Categories)
                {
                    Log("   Nome : " + category.Name + "; Pontuação : " + category.Score);
                }
            }

            if (result.Faces != null && result.Faces.Length > 0)
            {
                Log("Rostos : ");
                foreach (var face in result.Faces)
                {
                    Log("   Idade : " + face.Age + "; Sexo : " + face.Gender);
                }
            }

            if (result.Color != null)
            {
                Log("Cor acentuada : " + result.Color.AccentColor);
                Log("Cor dominante fundo : " + result.Color.DominantColorBackground);
                Log("Cor dominante primeiro plano : " + result.Color.DominantColorForeground);

                if (result.Color.DominantColors != null && result.Color.DominantColors.Length > 0)
                {
                    string colors = "Cores dominantes : ";
                    foreach (var color in result.Color.DominantColors)
                    {
                        colors += color + " ";
                    }
                    Log(colors);
                }
            }

            if (result.Description != null)
            {
                Log("Descrição : ");
                foreach (var caption in result.Description.Captions)
                {

                    Log("   Legenda : " + caption.Text + "; Confiança : " + caption.Confidence);
                }
                string tags = "   Tags : ";
                foreach (var tag in result.Description.Tags)
                {
                    tags += tag + ", ";
                }
                Log(tags);
            }

            if (result.Tags != null)
            {
                Log("Tags : ");
                foreach (var tag in result.Tags)
                {
                    //Log("   Nome : " + tag.Name + "; Confiança : " + tag.Confidence + "; Dica : " + tag.Hint);
                    Log("   Nome : " + tag.Name + "; Confiança : " + tag.Confidence);
                }
            }
        }

        private async Task<AnalysisResult> AnalyzeUrl(string imageUrl)
        {
            VisionServiceClient VisionServiceClient = new VisionServiceClient("SUA_CHAVE2_AQUI", "https://westcentralus.api.cognitive.microsoft.com/vision/v1.0");

            VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };
            AnalysisResult analysisResult = await VisionServiceClient.AnalyzeImageAsync(imageUrl, visualFeatures);
            return analysisResult;
        }

        private void Log(string msg)
        {
            if (msg != "")
                Console.WriteLine(" - " + msg);
        }
    }
}