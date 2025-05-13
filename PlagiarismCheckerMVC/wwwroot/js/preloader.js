document.addEventListener('DOMContentLoaded', () => {
    // Массив путей к изображениям, которые нужно предзагрузить
    const imagesToPreload = [
        '/images/plag-search-icon.png',
        '/images/llms-icon.png',
        '/images/plag-checker-logo.png'
    ];
    
    // Счетчик загруженных изображений
    let loadedImagesCount = 0;
    
    // Функция для проверки загрузки всех изображений
    const checkAllImagesLoaded = () => {
        loadedImagesCount++;
        if (loadedImagesCount === imagesToPreload.length) {
            // Все изображения загружены, скрываем прелоадер
            const preloader = document.getElementById('page-preloader');
            if (preloader) {
                preloader.style.opacity = '0';
                setTimeout(() => {
                    preloader.style.display = 'none';
                }, 200);
            }
        }
    };
    
    // Предзагрузка изображений
    imagesToPreload.forEach(src => {
        const img = new Image();
        img.src = src;
        img.onload = checkAllImagesLoaded;
        img.onerror = checkAllImagesLoaded; // Обрабатываем и ошибки загрузки
    });
    
    // Если изображения загружаются слишком долго, все равно скрываем прелоадер через 2 секунды
    setTimeout(() => {
        const preloader = document.getElementById('page-preloader');
        if (preloader) {
            preloader.style.opacity = '0';
            setTimeout(() => {
                preloader.style.display = 'none';
            }, 200);
        }
    }, 2000);
}); 