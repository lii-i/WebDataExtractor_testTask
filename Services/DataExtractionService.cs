using AngleSharp;
using AngleSharp.Dom;
using Npgsql;
using Dapper;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;

public class DataExtractionService{
    private IValidator<ExtractRequestDTO> _validator;
    private string _connectionString;
    private static readonly Regex EmailRegex = new Regex(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DataExtractionService(IValidator<ExtractRequestDTO> validator, string con_str){
        _validator = validator;
        _connectionString = con_str;
    }

    public async Task<ExtractResponseDTO> ProcessPayloadAsync(ExtractRequestDTO request){
        var validationResult = await _validator.ValidateAsync(request);

        if(!validationResult.IsValid){
            string errors = string.Join(",", validationResult.Errors);

            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "Ошибка валидации",
                ErrorMessage = errors
            };
        }

        ExtractResponseDTO response = new ExtractResponseDTO();

        string DecodeURL = string.Empty;
        try{
            DecodeURL = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.UrlB64));
        }catch(Exception e){
            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "Ошибка декодирования",
                ErrorMessage = "Ошибка при декодировании BASE 64 URL" + e.Message
            };
        }

        response.Url = DecodeURL;

        string DecodePage = string.Empty;
        try{
            DecodePage = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Page64));
        }catch(Exception e){
            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "Ошибка декодирования",
                ErrorMessage = "Ошибка при декодировании BASE 64 PAGE" + e.Message
            };
        }
        
        List<DbRecordDTO> recordsForDb = new List<DbRecordDTO>();
        
        try{
            
            var context = BrowsingContext.New(Configuration.Default);
            IDocument document = await context.OpenAsync(req => req.Content(DecodePage));
            IEnumerable<IElement> SelectorCollection = document.QuerySelectorAll(request.Selector);

            int count = 0;
            foreach(IElement e in SelectorCollection){
                string valueAttribute = e.GetAttribute(request.Attribute);
                if(valueAttribute != null){
                    response.ElementsAttrList.Add(valueAttribute);
                    recordsForDb.Add(new DbRecordDTO {Atribute_value = valueAttribute, ElementHtml = e.OuterHtml});
                }

                count ++;
            }
             response.ElementsCount = count;
        }catch(Exception e){
             return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "HTML_PARSE_ERROR",
               ErrorMessage = "Ошибка при разборе HTML-страницы или применении селектора: " + e.Message
            };
        }

        try{
            using(var connection = new NpgsqlConnection(_connectionString)){
                await connection.OpenAsync();

                string sqlQuery = @"INSERT INTO Elements(ATTRIBUTE, HTML) VALUES (@Atribute_value, @ElementHtml)";

                await connection.ExecuteAsync(sqlQuery, recordsForDb);
            }
        }catch(Exception e){
            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "DB_INSERT_ERROR",
                ErrorMessage = "Ошибка при записи в базу данных: " + e.Message
            };
        }


        try{
            MatchCollection emailMatches = EmailRegex.Matches(DecodePage);
            response.EmailsCount = emailMatches.Count;
            response.EmailsList = emailMatches.Select(m => m.Value).ToList();
        }catch(Exception e){
            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "EMAIL_REGEX_ERROR",
                ErrorMessage = "Ошибка при поиске email-адресов в содержимом страницы: " + e.Message
            };
        }

        using(Aes aes = Aes.Create()){
            
            byte[] KeyBates = Convert.FromBase64String(request.KeyBytesB64);
            byte[] EncryptedBytes = Convert.FromBase64String(request.EncryptedTextBytesB64);

            aes.Key = KeyBates;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

           using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                byte[] decryptedBytes = decryptor.TransformFinalBlock(EncryptedBytes, 0, EncryptedBytes.Length);
                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

                response.DecryptedPlainText = decryptedText;
            }
        }

        return response;

    }


}