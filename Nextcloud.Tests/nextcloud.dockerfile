FROM nextcloud:latest
ENV SQLITE_DATABASE=nextcloud-sqlite
ENV NEXTCLOUD_ADMIN_USER=admin
ENV NEXTCLOUD_ADMIN_PASSWORD=StrongPassword123!


# install Nextcloud via call to entrypoint.sh
RUN /entrypoint.sh apache2-foreground & pid=$! && sleep 60 && kill $pid

USER www-data

# install Nextcloud Apps
RUN cd /var/www/html && \
    php occ app:install deck && \
    php occ app:install spreed && \
    php occ app:install contacts && \
    php occ app:install collectives && \
    php occ app:install groupfolders

USER root
