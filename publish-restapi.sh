docker-compose build && \
docker-compose up -d && \
docker-compose logs --follow --tail="999" --timestamps restapi