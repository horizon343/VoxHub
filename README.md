<div align="center">

  <h1>🧊 VoxHub</h1>

  <p>
    <b>Desktop-клиент для управления, просмотра и версионирования voxel-моделей в формате <code>.vox</code>.</b>
  </p>

  <p>
    VoxHub помогает загружать voxel-модели, создавать коммиты, просматривать историю версий,
    скачивать модели и визуально сравнивать разные состояния прямо в интерфейсе.
  </p>

  <p>
    <img src="https://img.shields.io/badge/C%23-100%25-512BD4?style=for-the-badge&logo=csharp&logoColor=white" alt="C#">
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10">
    <img src="https://img.shields.io/badge/WPF-Desktop-0078D7?style=for-the-badge&logo=windows&logoColor=white" alt="WPF">
    <img src="https://img.shields.io/badge/gRPC-Client-2CA5E0?style=for-the-badge" alt="gRPC">
    <img src="https://img.shields.io/badge/Format-.vox-8A2BE2?style=for-the-badge" alt=".vox">
  </p>

  <p>
    <b>📦 Import</b> · <b>🌿 Versioning</b> · <b>🧱 3D Viewport</b> · <b>🔍 Diff</b>
  </p>

</div>

<hr>

<h2>✨ Возможности</h2>

<table>
  <tr>
    <td>📚 <b>Каталог моделей</b></td>
    <td>Просмотр списка voxel-моделей через gRPC-каталог.</td>
  </tr>
  <tr>
    <td>🕓 <b>История версий</b></td>
    <td>Отображение всех версий для выбранной модели.</td>
  </tr>
  <tr>
    <td>📤 <b>Snapshot</b></td>
    <td>Загрузка начального состояния модели из локального <code>.vox</code>-файла.</td>
  </tr>
  <tr>
    <td>🌿 <b>Commit</b></td>
    <td>Создание новой версии поверх выбранного состояния с сообщением коммита.</td>
  </tr>
  <tr>
    <td>📥 <b>Restore</b></td>
    <td>Скачивание выбранной версии обратно в формат <code>.vox</code>.</td>
  </tr>
  <tr>
    <td>🧱 <b>3D-просмотр</b></td>
    <td>Рендер voxel-модели во встроенном WPF viewport.</td>
  </tr>
  <tr>
    <td>🎮 <b>Камера</b></td>
    <td>Вращение мышью и масштабирование колесом.</td>
  </tr>
  <tr>
    <td>🕸️ <b>Граф версий</b></td>
    <td>Быстрый выбор нужного состояния модели.</td>
  </tr>
  <tr>
    <td>🆚 <b>Сравнение</b></td>
    <td>Открытие двух версий рядом в отдельных viewport-окнах.</td>
  </tr>
  <tr>
    <td>🔦 <b>Diff-подсветка</b></td>
    <td>Визуальное выделение отличающихся voxel-позиций между версиями.</td>
  </tr>
</table>

<hr>

<h2>🖼️ Что делает VoxHub</h2>

<p>
  <b>VoxHub</b> — это GUI-приложение для работы с voxel-моделями как с версионируемыми артефактами.
  Вместо ручного хранения множества <code>.vox</code>-файлов пользователь может загрузить модель как
  snapshot, затем создавать новые версии через commit, переключаться между ними, скачивать нужную
  версию и визуально сравнивать изменения.
</p>

<p>
  Приложение особенно полезно, когда voxel-модель развивается итеративно: например, при разработке
  игровых ассетов, 3D-прототипов, пиксельных сцен или объектов, где важно видеть историю изменений.
</p>

<hr>

<h2>🧩 Основной сценарий работы</h2>

<ol>
  <li>🚀 Пользователь открывает VoxHub.</li>
  <li>🔌 Приложение подключается к gRPC-сервису каталога моделей.</li>
  <li>📚 Список моделей загружается в интерфейс.</li>
  <li>🎯 После выбора модели отображается список её версий.</li>
  <li>🧱 Выбранную версию можно скачать и сразу отрендерить в 3D viewport.</li>
  <li>🌿 Новый <code>.vox</code>-файл можно загрузить как snapshot или commit.</li>
  <li>🆚 Любые две загруженные версии можно открыть рядом и сравнить визуально.</li>
</ol>

<hr>

<h2>🏗️ Архитектура проекта</h2>

<pre><code>VoxHub/
├── Domain/
│   ├── Canonical/      # Каноническое представление voxel-модели
│   ├── Importing/      # Импорт .vox-файлов
│   └── Vox/            # Работа с VOX-структурами
│
├── Interfaces/         # Общие интерфейсы приложения
│
├── Models/             # UI-модели и DTO для отображения
│
├── Protos/             # gRPC-контракты
│   ├── CommitImportService.proto
│   ├── ModelQueryService.proto
│   ├── ModelRestoreService.proto
│   ├── SnapshotImportService.proto
│   └── VersionQueryService.proto
│
├── Services/           # gRPC-клиенты и 3D-рендеринг
│   ├── GrpcVoxelCatalogService.cs
│   ├── IVoxelCatalogService.cs
│   └── VoxelViewportRenderer.cs
│
├── ViewModels/         # MVVM-логика главного окна
│   └── MainViewModel.cs
│
├── App.xaml            # Точка входа WPF-приложения
├── MainWindow.xaml     # Основной WPF-интерфейс
├── MainWindow.xaml.cs  # UI-события, viewport, diff и взаимодействие
├── VersionGraphControl.xaml
├── VersionGraphControl.xaml.cs
└── VoxHub.csproj       # Конфигурация .NET/WPF-проекта
</code></pre>

<hr>

<h2>⚙️ Технологии</h2>

<table>
  <thead>
    <tr>
      <th>Технология</th>
      <th>Для чего используется</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>💜 <b>C#</b></td>
      <td>Основной язык приложения</td>
    </tr>
    <tr>
      <td>🟣 <b>.NET 10.0 Windows</b></td>
      <td>Целевая платформа desktop-клиента</td>
    </tr>
    <tr>
      <td>🪟 <b>WPF</b></td>
      <td>Пользовательский интерфейс и 3D viewport</td>
    </tr>
    <tr>
      <td>🔌 <b>gRPC</b></td>
      <td>Обмен данными с backend-сервисом моделей</td>
    </tr>
    <tr>
      <td>📨 <b>Google.Protobuf</b></td>
      <td>Генерация и обработка protobuf-сообщений</td>
    </tr>
    <tr>
      <td>🧊 <b>.vox</b></td>
      <td>Формат импортируемых и экспортируемых voxel-моделей</td>
    </tr>
  </tbody>
</table>

<hr>

<h2>🔌 Backend-подключение</h2>

<p>
  VoxHub работает как desktop-клиент и ожидает совместимый gRPC backend.
  По умолчанию клиент подключается к сервису по адресу:
</p>

<pre><code>http://localhost:5152</code></pre>

<p>
  Через этот сервис приложение получает список моделей и версий,
  скачивает выбранные версии, загружает snapshot и отправляет commit.
</p>

<hr>

<h2>📦 Формат данных</h2>

<p>
  Приложение работает с файлами:
</p>

<pre><code>*.vox</code></pre>

<p>
  Такие файлы можно импортировать как начальный snapshot или использовать для создания commit.
  Скачанные версии также сохраняются в формате <code>.vox</code>.
</p>

<hr>

<h2>🚀 Быстрый старт</h2>

<p>
  Для запуска проекта нужно Windows-окружение, установленный .NET SDK с поддержкой
  <code>net10.0-windows</code> и запущенный совместимый gRPC backend на
  <code>http://localhost:5152</code>.
</p>

<pre><code>git clone https://github.com/horizon343/VoxHub.git
cd VoxHub
dotnet restore
dotnet build
dotnet run --project VoxHub/VoxHub.csproj</code></pre>

<hr>

<h2>📋 Требования</h2>

<ul>
  <li>🪟 Windows</li>
  <li>🟣 .NET SDK с поддержкой <code>net10.0-windows</code></li>
  <li>🔌 Совместимый VoxHub gRPC backend</li>
  <li>🧊 <code>.vox</code>-файлы для импорта, коммитов и просмотра</li>
</ul>

<hr>

<h2>🛠️ Статус проекта</h2>

<p>
  VoxHub находится в активной разработке. Основной фокус проекта — удобная работа с версиями
  voxel-моделей, визуальный просмотр и сравнение изменений.
</p>

<p>
  Возможные направления развития:
</p>

<ul>
  <li>🕸️ улучшение UX графа версий;</li>
  <li>🔎 расширенная фильтрация моделей и коммитов;</li>
  <li>📤 поддержка дополнительных форматов экспорта;</li>
  <li>🔦 улучшение diff-визуализации;</li>
  <li>📦 публикация готовых релизных сборок.</li>
</ul>

<hr>

<h2>🤝 Contributing</h2>

<p>
  Идеи, улучшения и pull requests приветствуются. Если вы хотите предложить новую функцию,
  улучшить визуализацию, оптимизировать импорт <code>.vox</code> или доработать gRPC-интеграцию —
  создайте issue или pull request.
</p>

<hr>

<div align="center">

  <p>
    <b>🧊 VoxHub</b> — удобный desktop-инструмент для управления историей voxel-моделей.
  </p>

  <p>
    <sub>Built with 💜 C#, WPF and voxel magic.</sub>
  </p>

</div>