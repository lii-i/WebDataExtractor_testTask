using FluentValidation;

public class ExtractionRequestDtoValidator : AbstractValidator<ExtractRequestDTO>{

    public ExtractionRequestDtoValidator(){
        RuleFor(er => er.Selector).NotEmpty();
        RuleFor(er => er.Attribute).NotEmpty();
        RuleFor(er => er.UrlB64).NotEmpty();
        RuleFor(er => er.EncryptedTextBytesB64).NotEmpty();
        RuleFor(er => er.KeyBytesB64).NotEmpty();
        RuleFor(er => er.Page64).NotEmpty();
    }
}