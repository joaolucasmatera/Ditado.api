# PER-65 · cadeia com três provedores

A cadeia ativa contém Groq, Cohere e OpenRouter. A ordem depende dos índices configurados. Cada provedor deve aparecer uma vez, com o respectivo modelo. A validação rejeita uma cadeia que ainda inclua GonkaBroker e o cliente recusa uma chamada direta a esse provedor antes do HTTP.

O Compose e `.env.docker.example` usam os índices 0 a 2 e as credenciais `GROQ_API_KEY`, `COHERE_API_KEY` e `OPENROUTER_API_KEY`. A mensagem de inicialização indica os três modelos esperados. Os registros históricos continuam disponíveis para leitura.

O `.env.production` local contém os quatro índices anteriores e a variável da credencial retirada. Para migrar, configure apenas os três provedores ativos nos índices 0 a 2, mova cada modelo junto com seu provedor e remova as entradas antigas do índice 3 e da credencial retirada. A coordenação vai aplicar a migração local.

O teste no navegador usou API 5064, frontend 5198 e o schema sintético `ditado_per65_tresprovedoresnov21`. Após a retomada da API, a mesma resposta exibiu classificação por código e LLM simulado. A nota permaneceu 33,33 e a consulta de outra escola recebeu HTTP 404. A trilha registrou uma chamada interrompida, uma concluída e três lacunas concluídas. As evidências estão em `work/logs/per65-tres-provedores-playwright.log` e `work/logs/per65-tres-provedores-trilha.log`.

O build terminou com zero erros e dois avisos preexistentes. Passaram 69 testes pertinentes. O teste do worker seguiu a ordem configurada Groq, OpenRouter e Cohere, com sucesso no terceiro provedor após HTTP 429 e timeout. Outro cenário confirmou o encerramento após três falhas técnicas. O Compose validou com as três credenciais esperadas. As chamadas a provedores reais ficam para a validação da coordenação. Os registros locais estão em `work/logs/per65-tres-provedores-build.log` e `work/logs/per65-tres-provedores.trx`.
