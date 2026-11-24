FROM node:24.11.1-bookworm-slim AS source
ARG ROUTER_REVISION=baf4e21c7b39692e143a85a307eb62490aa7bf6f
RUN apt-get update && apt-get install -y --no-install-recommends git ca-certificates \
    && git init /source \
    && git -C /source fetch --depth=1 https://github.com/nejib1/Free-LLM.git "$ROUTER_REVISION" \
    && git -C /source checkout --detach FETCH_HEAD \
    && test "$(git -C /source rev-parse HEAD)" = "$ROUTER_REVISION"

FROM node:24.11.1-bookworm-slim
WORKDIR /router
COPY --from=source /source/router/ ./
COPY iniciar-router.mjs consultar-router.mjs ./
RUN mkdir /state && chown node:node /state
USER node
ENTRYPOINT ["node", "iniciar-router.mjs"]
