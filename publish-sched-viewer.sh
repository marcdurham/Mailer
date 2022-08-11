docker-compose -f docker-compose.sched-viewer.yml build && \
docker-compose -f docker-compose.sched-viewer.yml up -d && \
docker image prune --force && \
docker-compose -f docker-compose.sched-viewer.yml logs --follow --tail="999" --timestamps sched-viewer