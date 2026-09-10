let map;
let clusters;

export function initialize(elementId, albums) {
    map = L.map(elementId, { worldCopyJump: true }).setView([25, 10], 2);
    L.tileLayer(
        "https://server.arcgisonline.com/ArcGIS/rest/services/Canvas/World_Light_Gray_Base/MapServer/tile/{z}/{y}/{x}",
        { attribution: "Tiles &copy; Esri", maxZoom: 16 }
    ).addTo(map);

    clusters = L.markerClusterGroup({
        showCoverageOnHover: false,
        spiderfyOnMaxZoom: true,
        maxClusterRadius: 55
    });
    map.addLayer(clusters);
    setAlbums(albums);
}

export function setAlbums(albums) {
    if (!map || !clusters) return;
    clusters.clearLayers();

    for (const album of albums) {
        if (!Number.isFinite(album.latitude) || !Number.isFinite(album.longitude)) continue;
        const marker = L.marker([album.latitude, album.longitude], { title: album.title });
        marker.bindTooltip(album.title);
        marker.on("click", () => window.open(album.publicUrl, "_blank", "noopener,noreferrer"));
        clusters.addLayer(marker);
    }

    if (clusters.getLayers().length > 0) {
        const bounds = clusters.getBounds();
        if (bounds.isValid()) map.fitBounds(bounds, { padding: [24, 24], maxZoom: 8 });
    }
}

export function dispose() {
    if (map) map.remove();
    map = undefined;
    clusters = undefined;
}
