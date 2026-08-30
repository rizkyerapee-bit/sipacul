FROM nginxinc/nginx-unprivileged:1.30.4-alpine

USER root
RUN apk add --no-cache openssl \
    && mkdir -p /etc/nginx/tls /etc/nginx/templates /etc/nginx/snippets \
    && chown -R 101:0 /etc/nginx/templates /etc/nginx/snippets \
    && chmod -R g+w /etc/nginx/templates /etc/nginx/snippets

COPY edge/default.conf /etc/nginx/templates/default.conf.template
COPY edge/25-sipacul-public-activation.sh /docker-entrypoint.d/25-sipacul-public-activation.sh
RUN chmod 0555 /docker-entrypoint.d/25-sipacul-public-activation.sh

USER 101

EXPOSE 8443
