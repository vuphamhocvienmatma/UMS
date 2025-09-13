using Nest;

namespace UMS.ES
{
    public static class ElasticsearchExtensions
    {
        public static void AddElasticsearch(
            this IServiceCollection services, IConfiguration configuration)
        {
            var url = configuration["Elasticsearch:Url"];
            var settings = new ConnectionSettings(new Uri(url))
                .PrettyJson()
                .ThrowExceptions();

            var client = new ElasticClient(settings);
            services.AddSingleton<IElasticClient>(client);
        }
    }
}
