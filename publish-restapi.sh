docker-compose build && \
docker-compose up -d && \
docker image prune --force && \
docker-compose logs --follow --tail="999" --timestamps restapi