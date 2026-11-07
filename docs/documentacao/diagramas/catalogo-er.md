# Marketplace — ER proposto, CAT-09

Revisão pendente; [contrato e decisões](../../epicos/catalogo-contrato.md). Não representa schema implementado.

```mermaid
erDiagram
    Usuario ||--o{ ErroCatalogo : publica
    Ditado ||--o{ DitadoErro : seleciona
    ErroCatalogo ||--o{ DitadoErro : integra
    RespostaSegmento ||--o{ RespostaSegmentoTipoErro : classifica
    ErroCatalogo o|--o{ RespostaSegmentoTipoErro : identifica
    ErroCatalogo {
        int id PK
        string codigo UK
        string nome
        string descricao
        string exemplos
        string orientacao
        string observacaoPedagogica
        string serieAno
        string regraCodigo "opcional; chave homologada proposta"
        int autorId FK
        bool ativo
        datetime criadoEm
        datetime alteradoEm
        datetime utilizadoEm "primeira ASSOCIACAO; nunca primeira resposta"
    }
    DitadoErro {
        int ditadoId PK,FK
        int erroCatalogoId PK,FK
    }
    RespostaSegmentoTipoErro {
        int respostaSegmentoId FK
        int tipoErro "enum existente preservado"
        int erroCatalogoId FK "opcional para legado; proposta pendente"
    }
```

Regra executável fica no domínio, sem tabela/editor de código livre. Conforme parecer final do gerente, cada erro programado possui item preparado no Marketplace com vínculo interno ao executor existente; professor seleciona itens e novos cadastros docentes não recebem executor. Q2/Q3 foram retiradas da fila humana, sem aliases, seletor de vínculo, cópias executáveis ou restrições globais novas de nomes/cardinalidade. Categoria/DitadoCategoria não integram este modelo. A referência de RespostaSegmentoTipoErro ao Marketplace recebeu parecer técnico do gerente; nullable preserva somente legados sem remapeamento automático. Toda nova classificação integrada deve guardar erroCatalogoId; schema ainda não implementado. Novas classificações vinculadas persistiriam a definição selecionada e o enum correspondente, conforme exemplo no contrato. Respostas não são apagadas quando uma seleção muda ou um erro é arquivado.

Primeira associação marca imutabilidade mesmo que seja removida antes de qualquer tentativa. `utilizadoEm` é proposta técnica para registrar esse fato; associar→desassociar não limpa o marco. Resposta humana recebida reforça edição/exclusão com arquivamento e preservação; efeito do executor nas novas tentativas não foi especificado. Lacuna destinada a CAT-04/CAT-05→COR-01 antes do consumo; não bloqueia preparar modelo/consulta/publicação CAT-01/02. Não representa aceite humano de risco nem aprovação global CAT-09.
