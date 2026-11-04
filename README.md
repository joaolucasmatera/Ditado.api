# Ditado API

API para gerenciamento e aplicação de atividades de ditado escolar. O sistema permite que administradores e professores cadastrem usuários, turmas, categorias e ditados, enquanto os alunos realizam as atividades e recebem correção e feedback sobre seus erros.

## Funcionalidades

- Autenticação e autorização com JWT.
- Cadastro e gerenciamento de usuários, com perfis de administrador, professor e aluno.
- Cadastro e gerenciamento de turmas e alunos.
- Criação, edição e categorização de ditados.
- Atribuição de ditados para turmas.
- Realização de ditados pelos alunos.
- Correção automática das respostas e classificação dos erros.
- Consulta de tentativas, resultados e evolução dos alunos.
- Relatórios de desempenho por aluno e turma.
- Documentação interativa da API com Swagger.

## Tecnologias

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core 9
- MySQL 8
- JWT Bearer Authentication
- Swagger / OpenAPI
- xUnit e FluentAssertions para testes
- GitHub Actions para build e deploy no Azure App Service

## Estrutura do projeto

```text
Ditado/
├── Ditado.API/          # API, controllers, autenticação e Swagger
├── Ditado.Aplicacao/    # Serviços de aplicação e DTOs
├── Ditado.Dominio/      # Entidades, enums e regras de domínio
├── Ditado.Infra/        # DbContext, configurações e migrations
├── Ditado.Testes/       # Testes unitários e de integração
└── Ditado.sln           # Solução .NET
```

## Requisitos

- .NET SDK 9.0 ou superior
- MySQL 8.0 ou superior
- Git

## Configuração local

1. Clone o repositório:

```bash
git clone https://github.com/joaolucasmatera/Ditado.api.git
cd Ditado
```

2. Crie um banco de dados MySQL para a aplicação:

```sql
CREATE DATABASE ditado;
```

3. Configure a string de conexão e as opções JWT em `Ditado.API/appsettings.Development.json` ou, preferencialmente, usando variáveis de ambiente/User Secrets.

Exemplo de configuração:

```json
{
  "ConnectionStrings": {
	"ConexaoPadrao": "Server=localhost;Port=3306;Database=ditado;User=ditadoUser;Password=SUA_SENHA;"
  },
  "Jwt": {
	"ChaveSecreta": "uma-chave-local-com-pelo-menos-32-caracteres",
	"Emissor": "DitadoAPI",
	"Audiencia": "DitadoClientes"
  }
}
```

> Não utilize senhas, chaves JWT ou outros segredos reais em arquivos versionados. O arquivo de configuração do repositório contém apenas valores de exemplo.

## Banco de dados e migrations

As migrations ficam no projeto `Ditado.Infra`. Em ambiente de desenvolvimento, aplique-as manualmente com:

```bash
dotnet ef database update --project Ditado.Infra --startup-project Ditado.API
```

Em ambientes diferentes de `Development` e `Testing`, a aplicação tenta aplicar as migrations automaticamente durante a inicialização.

## Executando a aplicação

Restaure as dependências e inicie a API:

```bash
dotnet restore
dotnet run --project Ditado.API
```

Por padrão, os endereços definidos pelo perfil de desenvolvimento são:

- HTTPS: `https://localhost:7036`
- HTTP: `http://localhost:5031`

A documentação Swagger estará disponível em:

- `https://localhost:7036/swagger`
- `http://localhost:5031/swagger`

Após realizar login, informe o token JWT no Swagger usando o botão **Authorize** no formato:

```text
Bearer SEU_TOKEN
```

## Principais grupos de endpoints

| Grupo | Rota base | Responsabilidade |
|---|---|---|
| Usuários | `/api/Usuarios` | Cadastro, login, aprovação de acesso e gerenciamento de usuários |
| Alunos | `/api/Alunos` | Ditados disponíveis e tentativas do aluno |
| Professores | `/api/Professores` | Ditados atribuídos e resultados das turmas |
| Turmas | `/api/Turmas` | Cadastro de turmas, alunos e atribuição de ditados |
| Ditados | `/api/Ditados` | Criação, edição, realização e submissão de respostas |
| Categorias | `/api/Categorias` | Cadastro e organização das categorias de ditado |

A especificação completa dos endpoints, parâmetros e modelos pode ser consultada no Swagger.

## Executando os testes

Para executar todos os testes:

```bash
dotnet test
```

Para compilar a solução:

```bash
dotnet build
```

## Deploy

O repositório possui um workflow em `.github/workflows/master_api-ditado.yml` que executa build e publicação quando há push no branch `master`, com deploy configurado para o Azure App Service `api-ditado`.

As credenciais e identificadores do Azure devem permanecer configurados como secrets no GitHub Actions.

## Escopo atual e próximos passos

O MVP é focado na aplicação online de ditados, correção automática e acompanhamento dos resultados. Entre as evoluções previstas estão OCR para respostas manuscritas, suporte mobile, arquitetura multi-tenant e recursos avançados de análise de padrões de erro.
