public void EndPoints(this WebApplication app){

    app.MapPost("/api/extractor", (
        [FromBody] ExtractRequestDTO request
        [FromService]  
    )=>{

    });



}