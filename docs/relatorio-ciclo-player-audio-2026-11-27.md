# Controles de áudio do ditado · 27/11/2026

A mudança facilita acompanhar o ditado no ritmo que o aluno precisa. O mesmo botão alterna entre **Tocar áudio** e **Pausar áudio**, com ícone e nome acessível correspondentes. Ao retomar, a reprodução continua na posição da pausa. O seletor oferece **Mais devagar (0,75×)**, **Normal (1×)** e **Mais rápido (1,25×)**, com **Normal** selecionado ao abrir o ditado.

A velocidade é aplicada a `HTMLMediaElement.playbackRate`, no próprio navegador. O áudio recebido pela API é usado como fonte, e o player acompanha os eventos do elemento para manter o estado em sincronia. Ao terminar, ele volta ao início. O commit de frontend `c5df3b1` da branch `codex/player-audio-ditado` altera `src/views/RealizarDitado.vue` e acrescenta `tests/e2e/player-audio-ditado.e2e.js`.

## Validação

- `npm run build` passou com Vite 5.4.21, com avisos de depreciação da API Node em CommonJS e tamanho do bundle.
- `node tests/e2e/player-audio-ditado.e2e.js` passou no Chrome em preview isolado do frontend compilado, com viewport de 360 px. O teste gerou áudio WAV sintético e conferiu nome e foco do botão, Enter e Espaço, as três opções do seletor, a taxa real do elemento, a posição ao pausar e retomar, o fim da reprodução e os controles desativados quando falta áudio.
- A API da sessão de teste foi simulada. A publicação e a validação independente de QA seguem pendentes.

A busca no Linear não encontrou uma tarefa correspondente a esta melhoria. A evidência fica neste relatório para vinculação quando houver uma tarefa identificada.
