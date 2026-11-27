# Avaliação do Free-LLM Router para o Ditado

A migração proposta está no backlog da [PER-69](https://linear.app/joaomoutinho/issue/PER-69/migrar-a-rotacao-da-correcao-llm-para-o-free-llm-router), vinculada à PER-63 e relacionada à PER-65.

### Diretriz mais recente do usuário

Dar preferência aos modelos com melhor desempenho nos ensaios e aceitar a troca para outros modelos disponíveis quando os primeiros estiverem limitados. Uma resposta aceita deve registrar o modelo que a gerou. A lista fechada de modelos e a manutenção da rotação na API eram propostas do primeiro piloto. A política indicada para a integração passa a ser rotação no Router, validação do lote na API e registro do provedor e modelo efetivos. Essa integração ainda está em avaliação.

Consulta em 22/11/2026. O usuário propôs centralizar credenciais e facilitar a escolha de provedores e modelos. Foi avaliado o commit `baf4e21c7b39692e143a85a307eb62490aa7bf6f`, versão 1.0.1. A adoção permanece em avaliação. [Código e documentação da revisão](https://github.com/nejib1/Free-LLM/tree/baf4e21c7b39692e143a85a307eb62490aa7bf6f/router)

O Router oferece painel local de credenciais e modelos, endpoint compatível com OpenAI e rotação por disponibilidade. O ranking considera atributos gerais dos modelos, latência e disponibilidade. A avaliação de concordância, cobertura e falsos positivos continua baseada nas amostras rotuladas do Ditado. [Critério pedagógico aprovado](../documentacao/evidencias/per65-indice-ensaios.md)

## Verificação com dados sintéticos

Os 25 testes do projeto passaram com um provedor simulado em localhost. Uma verificação adicional com o payload v4 sintético confirmou que o primeiro pedido preserva `response_format`. Na repetição de compatibilidade, o Router retira esse campo e mantém o texto da tentativa. O retorno de sucesso informa o provedor efetivo, o modelo e o número de tentativas nos cabeçalhos. A trilha atual do Ditado registra desfecho e motivo por tentativa de provedor. Para trocar de operador dessa rotação, é preciso verificar a recuperação do histórico completo e sua associação com cada lote. [Construção da requisição](https://github.com/nejib1/Free-LLM/blob/baf4e21c7b39692e143a85a307eb62490aa7bf6f/router/src/upstream.js) · [Cabeçalhos de resposta](https://github.com/nejib1/Free-LLM/blob/baf4e21c7b39692e143a85a307eb62490aa7bf6f/router/src/adapters/openai.js)

O código de ranking ordena os modelos da cadeia e mantém outros modelos elegíveis depois deles. A resolução de um ID desconhecido pode cair no perfil padrão. Para o Ditado, o modelo escolhido precisa estar restrito antes do envio. Conferir a cobertura do lote depois da chamada continua obrigatório. [Resolução e ranking](https://github.com/nejib1/Free-LLM/blob/baf4e21c7b39692e143a85a307eb62490aa7bf6f/router/src/router.js)

## Proposta para o piloto

Executar a revisão fixa em um container separado, na rede interna do Compose, com volume privado para a configuração e painel acessível apenas localmente. Usar dados sintéticos, desativar convidados e catálogo dinâmico para o perfil do Ditado. Confirmar que apenas os provedores e modelos autorizados são selecionáveis e que um modelo ausente resulta em erro antes de enviar o ditado. Demonstrar a manutenção do schema ou a rejeição da chamada se o gateway não puder garanti-lo. Registrar provedor, modelo e cada tentativa associada ao lote.

O primeiro piloto pode usar o Router para chaves e encaminhamento a modelos fixados, com a API responsável pela ordem de fallback, validação integral e registro dos resultados. Transferir a rotação para o Router é uma decisão posterior, condicionada aos testes de paridade da trilha e do comportamento em falhas. O GonkaBroker continua fora da cadeia aprovada. Chaves reais e respostas escolares ficam para o ambiente autorizado após a validação do piloto.
