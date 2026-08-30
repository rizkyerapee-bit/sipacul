FROM nginxinc/nginx-unprivileged:1.30.4-alpine

USER root
RUN apk add --no-cache openssl \
    && mkdir -p /etc/nginx/tls

COPY edge/default.conf /etc/nginx/conf.d/default.conf

USER 101

EXPOSE 8443
