docker-compose -f docker-compose.sched-viewer-api.yml build sched-viewer-api && \
docker-compose -f docker-compose.sched-viewer-api.yml up -d sched-viewer-api && \
docker image prune --force && \
docker-compose -f docker-compose.sched-viewer-api.yml logs --follow --tail="999" --timestamps sched-viewer-api