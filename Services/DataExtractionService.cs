using AngleSharp;
using AngleSharp.Dom;
using Npgsql;
using Dapper;
using System.Security.Cryptography

public class DataExtractionService{
    private ExtractionRequestDtoValidator _validator;
    private string _connectionString;
    private static readonly Regex EmailRegex = new Regex(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", 
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DataExtractionService(ExtractionRequestDtoValidator validator, string con_str){
        _validator = validator;
        _connectionString = con_str;
    }

    public async Task<ExtractResponseDTO> ProcessPayloadAsync(ExtractRequestDto request){
        var validationResult = await _validator.ValidateAsync();

        if(!validationResult.IsValid){
            string errors = string.Join(",", validationResult.Errors);

            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "Ошибка валидации",
                ErrorMessage = errors
            };
        }

        string DecodeURL = string.Empty;
        try{
            string DecodeURL = System.Text.Encoding.UTF8.GetString(Convert.FromBase64(request.UrlB64));
        }catch(Exeption e){
            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "Ошибка декодирования",
                ErrorMessage = "Ошибка при декодировании BASE 64 URL" + e.Message;
            };
        }

        string DecodePage = string.Empty;
        try{
            string DecodePage = System.Text.Encoding.UTF8.GetString(Convert.FromBase64(request.Page64));
        }catch(Exeption e){
            return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "Ошибка декодирования",
                ErrorMessage = "Ошибка при декодировании BASE 64 PAGE" + e.Message;
            };
        }
        
        ExtractionResponseDto response = new ExtractionResponseDto();
        List<DbRecordDTO> recordsForDb = new List<DbRecordDTO>();
        
        try{
            
            var context = BrowsingContext.New(Configuration.Default);
            IDocument document = await context.OpenAsync(req => req.Content(DecodePage));
            IEnumerable<TElement> SelectorCollection = document.QuerySelectorAll(request.Selector);

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
        }catch(Exeption e){
             return new ExtractResponseDTO {
                IsError = 1,
                ErrorCode = "Ошибка чтения строки документа",
                ErrorMessage = "Ошибка чтения декодированной страницы или селектора" + e.Message;
            };
        }

        using(var connection = new NpgsqlConnection(_connectionString)){
            await connection.OpenAsync();

            string sqlQuery = @"INSERT INTO Elements(atribute_value, html_page) VALUES (@Atribute_value, @ElementHtml)";

            await connection.ExecuteAsync(sqlQuery, recordsForDb);
        }

        MatchCollection emailMatches = EmailRegex.Matches(DecodePage);
        response.EmailsCount = emailMatches.Count();
        response.EmailsList = emailMatches.Select(m => m.Value).Distinct().ToList();

        using(Aes aes = Aes.Creating()){
            
            bytes[] KeyBates = Convert.FromBase64String(request.KeyBytesB64);
            bytes[] EncryptedBytes = Convert.FromBase64String(requst.EncryptedTextBytesB64);

            aes.Key = KeyBates;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

           using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                byte[] decryptedBytes = decryptor.TransformFinalBlock(EncryptedBytes, 0, EncryptedBytes.Length);
                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

                response.DecryptedText = decryptedText;
            }
        }
        

    }


}