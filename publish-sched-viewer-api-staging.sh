docker-compose -f docker-compose.sched-viewer-api-staging.yml build sched-viewer-api-staging && \
docker-compose -f docker-compose.sched-viewer-api-staging.yml up -d sched-viewer-api-staging && \
docker image prune --force && \
docker-compose -f docker-compose.sched-viewer-api-staging.yml logs --follow --tail="999" --timestamps sched-viewer-api-staging