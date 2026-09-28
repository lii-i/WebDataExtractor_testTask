public static class MapEndPoints{

public static void EndPoints(this WebApplication app){

    app.MapPost("/api/extractor", async (
        ExtractRequestDTO request,
        DataExtractionService deService
    )=>{
        ExtractResponseDTO response = await deService.ProcessPayloadAsync(request); 

        if(response.IsError == 1) return Results.BadRequest(response);

        return Results.Ok(response);
    });

}

}