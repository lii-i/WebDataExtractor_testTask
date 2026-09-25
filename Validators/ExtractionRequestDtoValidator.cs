using FluentValidation;

public class ExtractionRequestDtoValidator : AbstractValidator<ExtractionRequestDto>{

    public ExtractionRequestDtoValidator(){
        RoleFor(er => er.Selector).NotEmpty();
        RoleFor(er => er.Attribute).NotEmpty();
        RoleFor(er => er.UrlB64).NotEmpty();
        RoleFor(er => er.EncryptedTextBytesB64).NotEmpty();
        RoleFor(er => er.KeyBytesB64).NotEmpty();
        RoleFor(er => er.Page64).NotEmpty();
    }
}