-- Create Database
-- Run this separately: CREATE DATABASE fulltextsearchdb;

-- Connect to the database
-- \c fulltextsearchdb

-- Create Articles Table
CREATE TABLE articles (
    article_id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    author VARCHAR(100),
    category VARCHAR(50),
    published_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_modified TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create Categories Table
CREATE TABLE categories (
    category_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255)
);

-- Create Tags Table
CREATE TABLE tags (
    tag_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

-- Create ArticleTags Junction Table
CREATE TABLE article_tags (
    article_id INTEGER NOT NULL,
    tag_id INTEGER NOT NULL,
    PRIMARY KEY (article_id, tag_id),
    FOREIGN KEY (article_id) REFERENCES articles(article_id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags(tag_id) ON DELETE CASCADE
);

-- Add tsvector column for full-text search
ALTER TABLE articles ADD COLUMN search_vector tsvector;
CREATE INDEX articles_search_idx ON articles USING GIN (search_vector);

-- Create function to update search vector
CREATE OR REPLACE FUNCTION articles_search_vector_update() RETURNS trigger AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('english', COALESCE(NEW.title, '')), 'A') ||
        setweight(to_tsvector('english', COALESCE(NEW.content, '')), 'B') ||
        setweight(to_tsvector('english', COALESCE(NEW.author, '')), 'C') ||
        setweight(to_tsvector('english', COALESCE(NEW.category, '')), 'D');
    RETURN NEW;
END
$$ LANGUAGE plpgsql;

-- Create trigger to update search vector
CREATE TRIGGER articles_search_vector_update
BEFORE INSERT OR UPDATE ON articles
FOR EACH ROW EXECUTE FUNCTION articles_search_vector_update();

-- Insert Sample Categories
INSERT INTO categories (name, description) VALUES 
('Technology', 'Articles about technology, software, and hardware'),
('Science', 'Scientific articles and research'),
('Programming', 'Programming tutorials and tips'),
('Web Development', 'Articles about web development'),
('Data Science', 'Articles about data science and analytics'),
('Mobile Development', 'Articles about mobile app development'),
('DevOps', 'Articles about DevOps practices and tools');

-- Insert Sample Tags
INSERT INTO tags (name) VALUES 
('C#'), ('ASP.NET'), ('MVC'), ('PostgreSQL'), ('Full-Text Search'),
('JavaScript'), ('HTML'), ('CSS'), ('Database'), ('Entity Framework'),
('LINQ'), ('Azure'), ('Cloud Computing'), ('AI'), ('Machine Learning'),
('Python'), ('React'), ('Angular'), ('Node.js'), ('Docker');

-- Insert 20 Sample Articles
INSERT INTO articles (title, content, author, category, published_date) VALUES
('Introduction to C#', 
'C# is a modern, object-oriented programming language developed by Microsoft. It was designed for building a variety of applications that run on the .NET Framework. C# is simple, powerful, type-safe, and object-oriented.

C# syntax is highly expressive, yet it is also simple and easy to learn. The curly-brace syntax of C# will be instantly recognizable to anyone familiar with C, C++, or Java. Developers who know any of these languages are typically able to begin to work productively in C# within a short time.', 
'John Smith', 'Programming', '2025-01-15'),

('ASP.NET Core MVC Fundamentals', 
'ASP.NET Core MVC is a rich framework for building web apps and APIs using the Model-View-Controller design pattern. The Model-View-Controller (MVC) architectural pattern separates an application into three main groups of components: Models, Views, and Controllers.

Models are classes that represent the data of the application. The model classes use validation logic to enforce business rules for that data. Typically, model objects retrieve and store model state in a database.', 
'Jane Doe', 'Web Development', '2025-02-20'),

('Full-Text Search in PostgreSQL', 
'PostgreSQL provides powerful full-text search capabilities that allow you to identify natural-language documents that satisfy a query and optionally to sort them by relevance to the query.

The most common type of search is to find all documents containing specified query terms and return them in order of their similarity to the query. PostgreSQL provides the @@ operator to test whether a document matches a query, and the ts_rank and ts_rank_cd functions to calculate the similarity of a document to a query.', 
'Michael Johnson', 'Database', '2025-03-10'),

('Entity Framework Core Basics', 
'Entity Framework Core is a modern object-database mapper for .NET. It supports LINQ queries, change tracking, updates, and schema migrations. EF Core works with SQL Server, Azure SQL Database, SQLite, Azure Cosmos DB, MySQL, PostgreSQL, and other databases through a provider plugin API.

Entity Framework Core serves as an object-relational mapper (O/RM), which enables .NET developers to work with a database using .NET objects, eliminating the need for most of the data-access code they usually need to write.', 
'Sarah Williams', 'Programming', '2025-04-05'),

('JavaScript Fundamentals', 
'JavaScript is a lightweight, interpreted programming language with object-oriented capabilities. It is widely used for enhancing the interaction of a user with the webpage. In other words, you can make your webpage more lively and interactive, with the help of JavaScript.

JavaScript was initially created to "make web pages alive". The programs in this language are called scripts. They can be written right in a web page''s HTML and run automatically as the page loads.', 
'David Brown', 'Web Development', '2025-05-12'),

('Introduction to Python', 
'Python is an interpreted, high-level, general-purpose programming language. Its design philosophy emphasizes code readability with its use of significant whitespace. Python is dynamically typed and garbage-collected.

Python supports multiple programming paradigms, including structured, object-oriented, and functional programming. It is often described as a "batteries included" language due to its comprehensive standard library.', 
'Emily Chen', 'Programming', '2025-01-25'),

('React.js for Beginners', 
'React is a JavaScript library for building user interfaces. It is maintained by Facebook and a community of individual developers and companies. React can be used as a base in the development of single-page or mobile applications.

React allows developers to create large web applications that can change data, without reloading the page. The main purpose of React is to be fast, scalable, and simple.', 
'Robert Kim', 'Web Development', '2025-02-05'),

('Machine Learning Basics', 
'Machine learning is a method of data analysis that automates analytical model building. It is a branch of artificial intelligence based on the idea that systems can learn from data, identify patterns and make decisions with minimal human intervention.

The iterative aspect of machine learning is important because as models are exposed to new data, they are able to independently adapt. They learn from previous computations to produce reliable, repeatable decisions and results.', 
'Lisa Wong', 'Data Science', '2025-03-20'),

('Docker Containers Explained', 
'Docker is a platform for developers and sysadmins to develop, deploy, and run applications with containers. The use of Linux containers to deploy applications is called containerization.

Containers are not new, but their use for easily deploying applications is. Containerization is increasingly popular because containers are:
- Flexible: Even the most complex applications can be containerized.
- Lightweight: Containers leverage and share the host kernel.
- Interchangeable: You can deploy updates and upgrades on-the-fly.', 
'Mark Thompson', 'DevOps', '2025-04-15'),

('Angular vs React: A Comparison', 
'Angular and React are two of the most popular JavaScript frameworks for building web applications. Angular is a complete framework, while React is a library focused on the view layer.

Angular uses TypeScript, which brings strong typing to JavaScript, while React uses JSX, a syntax extension that allows you to write HTML in your JavaScript. Both have their strengths and weaknesses, and the choice between them often comes down to project requirements and team preferences.', 
'Jennifer Lee', 'Web Development', '2025-05-25'),

('Cloud Computing Fundamentals', 
'Cloud computing is the delivery of computing services—including servers, storage, databases, networking, software, analytics, and intelligence—over the Internet ("the cloud") to offer faster innovation, flexible resources, and economies of scale.

You typically pay only for cloud services you use, helping you lower your operating costs, run your infrastructure more efficiently, and scale as your business needs change.', 
'Daniel Martinez', 'Cloud Computing', '2025-01-30'),

('Introduction to LINQ', 
'LINQ (Language Integrated Query) is a set of features introduced in .NET 3.5 that bridges the gap between the world of objects and the world of data. LINQ provides a consistent query experience for objects in memory, databases, XML, and various other data sources.

LINQ allows you to query any collection implementing IEnumerable<T>, such as arrays, lists, and dictionaries. It also provides a consistent model for working with data across various kinds of data sources and formats.', 
'Amanda Wilson', 'Programming', '2025-02-15'),

('Mobile App Development with Xamarin', 
'Xamarin is a Microsoft-owned software company that provides tools for cross-platform mobile app development. Xamarin allows developers to share code across multiple platforms while still delivering native performance and user experience.

With Xamarin, you can write your app in C# and compile it to native code for iOS, Android, and Windows. This approach allows for code sharing while still taking advantage of platform-specific features.', 
'Brian Taylor', 'Mobile Development', '2025-03-25'),

('Introduction to Neural Networks', 
'Neural networks are a set of algorithms, modeled loosely after the human brain, that are designed to recognize patterns. They interpret sensory data through a kind of machine perception, labeling or clustering raw input.

The patterns they recognize are numerical, contained in vectors, into which all real-world data, be it images, sound, text or time series, must be translated. Neural networks help us cluster and classify data.', 
'Sophia Garcia', 'Data Science', '2025-04-20'),

('Microservices Architecture', 
'Microservices architecture is an approach to building applications where the application is broken down into smaller, independent services that communicate over a network. Each service focuses on a specific business capability and can be developed, deployed, and scaled independently.

This architectural style has gained popularity as organizations look to improve agility and move towards DevOps and continuous delivery. Microservices allow teams to work independently on different parts of the application.', 
'Kevin Nguyen', 'DevOps', '2025-05-30'),

('CSS Grid Layout', 
'CSS Grid Layout is a two-dimensional layout system for the web. It lets you lay out items in rows and columns, and has many features that make building complex layouts straightforward.

Grid Layout provides a mechanism for authors to divide available space for layout into columns and rows using a set of predictable sizing behaviors. Elements can be placed onto the grid, spanning multiple columns or rows.', 
'Rachel Adams', 'Web Development', '2025-01-10'),

('Blockchain Technology', 
'Blockchain is a distributed ledger technology that allows data to be stored globally on thousands of servers while letting anyone on the network see everyone else''s entries in near real-time. This makes it difficult for one user to gain control of the network.

The blockchain is a continuously growing list of records, called blocks, which are linked and secured using cryptography. Each block typically contains a cryptographic hash of the previous block, a timestamp, and transaction data.', 
'Thomas White', 'Technology', '2025-02-25'),

('Quantum Computing', 
'Quantum computing is a type of computation that harnesses the collective properties of quantum states, such as superposition, interference, and entanglement, to perform calculations. The devices that perform quantum computations are known as quantum computers.

Quantum computers are believed to be able to solve certain computational problems, such as integer factorization, substantially faster than classical computers. The study of quantum computing is a subfield of quantum information science.', 
'Laura Robinson', 'Science', '2025-03-15'),

('Responsive Web Design', 
'Responsive web design is an approach to web design that makes web pages render well on a variety of devices and window or screen sizes. Content, design, and performance are necessary across all devices to ensure usability and satisfaction.

A responsive design adapts the layout to the viewing environment by using fluid, proportion-based grids, flexible images, and CSS3 media queries. This approach aims to develop sites to provide an optimal viewing experience.', 
'Steven Clark', 'Web Development', '2025-04-25');

-- Associate Articles with Tags
INSERT INTO article_tags (article_id, tag_id) VALUES
(1, 1), 
(1, 10), 
(2, 2),
(2, 3), 
(3, 4),
(3, 5),
(3, 9), 
(4, 1),
(4, 10),
(4, 11),
(5, 6),
(5, 7),
(5, 8),
(6, 16),
(7, 17),
(7, 6),
(8, 14),
(8, 15), 
(9, 20),
(10, 17),
(10, 18),
(11, 13),
(11, 12),
(12, 11),
(12, 1),
(13, 1),
(14, 14),
(14, 15),
(15, 20),
(16, 8),
(16, 7);


-- INSERT INTO article_tags (article_id, tag_id) VALUES
-- (1, 1), -- C# article with C# tag
-- (1, 10), -- C# article with Entity Framework tag
-- (2, 2), -- ASP.NET article with ASP.NET tag
-- (2, 3), -- ASP.NET article with MVC tag
-- (3, 4), -- PostgreSQL article with PostgreSQL tag
-- (3, 5), -- PostgreSQL article with Full-Text Search tag
-- (3, 9), -- PostgreSQL article with Database tag
-- (4, 1), -- Entity Framework article with C# tag
-- (4, 10), -- Entity Framework article with Entity Framework tag
-- (4, 11), -- Entity Framework article with LINQ tag
-- (5, 6), -- JavaScript article with JavaScript tag
-- (5, 7), -- JavaScript article with HTML tag
-- (5, 8), -- JavaScript article with CSS tag
-- (6, 16), -- Python article with Python tag
-- (7, 17), -- React article with React tag
-- (7, 6), -- React article with JavaScript tag
-- (8, 14), -- Machine Learning article with AI tag
-- (8, 15), -- Machine Learning article with Machine Learning tag
-- (9, 20), -- Docker article with Docker tag
-- (10, 17), -- Angular vs React article with React tag
-- (10, 18), -- Angular vs React article with Angular tag
-- (11, 13), -- Cloud Computing article with Cloud Computing tag
-- (11, 12), -- Cloud Computing article with Azure tag
-- (12, 11), -- LINQ article with LINQ tag
-- (12, 1), -- LINQ article with C# tag
-- (13, 1), -- Xamarin article with C# tag
-- (14, 14), -- Neural Networks article with AI tag
-- (14, 15), -- Neural Networks article with Machine Learning tag
-- (15, 20), -- Microservices article with Docker tag
-- (16, 8), -- CSS Grid article with CSS tag
-- (16, 7), -- CSS Grid article with HTML tag
-- (17, 9), -- Blockchain article with Database tag
-- (18, 2), -- Quantum Computing article with Science tag
-- (19, 7), -- Responsive Web Design article with HTML tag
-- (19, 8), -- Responsive Web Design article with CSS tag
-- (20, 7), -- Responsive Web Design article with HTML tag
-- (20, 8); -- Responsive Web Design article with CSS tag

-- Sample Full-Text Search Queries for PostgreSQL

-- 1. Basic full-text search
-- SELECT title, author
-- FROM articles
-- WHERE search_vector @@ to_tsquery('english', 'programming');

-- 2. Ranking results by relevance
-- SELECT title, author, ts_rank(search_vector, query) AS rank
-- FROM articles, to_tsquery('english', 'web & development') query
-- WHERE search_vector @@ query
-- ORDER BY rank DESC;

-- 3. Highlighting search results
-- SELECT title, author, 
--   ts_headline('english', content, to_tsquery('english', 'database'),
--     'StartSel = <mark>, StopSel = </mark>, MaxWords=35, MinWords=15')
-- FROM articles
-- WHERE search_vector @@ to_tsquery('english', 'database');

-- 4. Combining full-text search with other conditions
-- SELECT a.title, a.author
-- FROM articles a
-- JOIN article_tags at ON a.article_id = at.article_id
-- JOIN tags t ON at.tag_id = t.tag_id
-- WHERE a.search_vector @@ to_tsquery('english', 'development')
-- AND t.name = 'JavaScript';

-- 5. Phrase search
-- SELECT title, author
-- FROM articles
-- WHERE search_vector @@ phraseto_tsquery('english', 'machine learning');

-- 6. Using logical operators
-- SELECT title, author
-- FROM articles
-- WHERE search_vector @@ to_tsquery('english', 'web & (development | design) & !mobile');


-- Thai Articles with Full-Text Search Support

-- Create Thai Articles Table
CREATE TABLE thai_articles (
    article_id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    content TEXT NOT NULL,
    author VARCHAR(100),
    category VARCHAR(50),
    published_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_modified TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create Categories Table
CREATE TABLE categories_thai (
    category_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255)
);

-- Create Tags Table
CREATE TABLE tags_thai (
    tag_id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255)
);

-- Create ArticleTags Junction Table for Thai Articles
CREATE TABLE thai_article_tags (
    article_id INTEGER NOT NULL,
    tag_id INTEGER NOT NULL,
    PRIMARY KEY (article_id, tag_id),
    FOREIGN KEY (article_id) REFERENCES thai_articles(article_id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags_thai(tag_id) ON DELETE CASCADE
);

-- Add tsvector column for Thai full-text search
ALTER TABLE thai_articles ADD COLUMN search_vector tsvector;
CREATE INDEX thai_articles_search_idx ON thai_articles USING GIN (search_vector);

-- Create function to update Thai search vector
-- Note: Using 'simple' configuration since PostgreSQL doesn't have built-in Thai dictionary
-- For production, consider installing and configuring proper Thai dictionaries
CREATE OR REPLACE FUNCTION thai_articles_search_vector_update() RETURNS trigger AS $$
BEGIN
    NEW.search_vector :=
        setweight(to_tsvector('simple', COALESCE(NEW.title, '')), 'A') ||
        setweight(to_tsvector('simple', COALESCE(NEW.content, '')), 'B') ||
        setweight(to_tsvector('simple', COALESCE(NEW.author, '')), 'C') ||
        setweight(to_tsvector('simple', COALESCE(NEW.category, '')), 'D');
    RETURN NEW;
END
$$ LANGUAGE plpgsql;

INSERT INTO categories_thai (name, description) VALUES 
('อาหารจานด่วน', 'บทความเกี่ยวกับอาหารจานด่วน'),
('อาหารทะเล', 'บทความเกี่ยวกับอาหารทะเล'),
('อาหารภาคกลาง', 'บทความเกี่ยวกับอาหารภาคกลาง'),
('อาหารจานด่วน', 'บทความเกี่ยวกับอาหารจานด่วน'),
('อาหารภาคเหนือ', 'บทความเกี่ยวกับอาหารภาคเหนือ'),
('อาหารภาคอีสาน', 'บทความเกี่ยวกับอาหารภาคอีสาน'),
('อาหารภาคอีสาน', 'บทความเกี่ยวกับอาหารภาคอีสาน'),
('อาหารภาคใต้', 'บทความเกี่ยวกับอาหารภาคใต้'),
('สตรีทฟู้ด', 'บทความเกี่ยวกับสตรีทฟู้ด'),
('อาหารภาคอีสาน', 'บทความเกี่ยวกับอาหารภาคอีสาน'),
('อาหารภาคใต้', 'บทความเกี่ยวกับอาหารภาคใต้'),
('อาหารจานด่วน', 'บทความเกี่ยวกับอาหารจานด่วน'),
('อาหารพื้นบ้าน', 'บทความเกี่ยวกับอาหารพื้นบ้าน'),
('อาหารทะเล', 'บทความเกี่ยวกับอาหารทะเล'),
('ของหวาน', 'บทความเกี่ยวกับของหวาน'),
('ของกินเล่น', 'บทความเกี่ยวกับของกินเล่น'),
('อาหารมังสวิรัติ', 'บทความเกี่ยวกับอาหารมังสวิรัติ'),
('ขนมไทย', 'บทความเกี่ยวกับขนมไทย'),
('ของหวาน', 'บทความเกี่ยวกับของหวาน'),
('อาหารสุขภาพ', 'บทความเกี่ยวกับอาหารสุขภาพ');


-- Insert Sample Tags
INSERT INTO tags_thai (name) VALUES 
('อาหารจานด่วน'), ('อาหารภาคเหนือ'), ('อาหารภาคกลาง'), ('อาหารภาคใต้'),
('อาหารทะเล'), ('อาหารพื้นบ้าน'), ('สตรีทฟู้ด'), ('ของหวาน'), ('ของกินเล่น'),
('อาหารมังสวิรัติ'), ('อาหารสุขภาพ');

-- Create trigger to update Thai search vector
CREATE TRIGGER thai_articles_search_vector_update
BEFORE INSERT OR UPDATE ON thai_articles
FOR EACH ROW EXECUTE FUNCTION thai_articles_search_vector_update();

-- Insert Sample Thai Articles
INSERT INTO thai_articles (title, content, author, category, published_date) VALUES
('ข้าวมันไก่ สูตรต้นตำรับ', 
'ข้าวมันไก่เป็นอาหารจานเดียวที่ได้รับความนิยมทั่วไทย เสิร์ฟพร้อมน้ำจิ้มรสจัดและน้ำซุปไก่ใสร้อนๆ ถือเป็นเมนูง่ายๆ แต่อร่อยและอิ่มท้อง', 
'สมศักดิ์ ชิมดี', 'อาหารจานด่วน', '2025-01-01'),

('ต้มยำกุ้ง รสชาติไทยแท้', 
'ต้มยำกุ้งเป็นเมนูขึ้นชื่อของไทย มีรสเผ็ด เปรี้ยว หอมสมุนไพร เช่น ตะไคร้ ใบมะกรูด และพริกสด นิยมใส่กุ้งตัวใหญ่เพื่อความกลมกล่อม', 
'สมหญิง รักกิน', 'อาหารทะเล', '2025-01-03'),

('แกงเขียวหวานไก่ หอมกะทิกลมกล่อม', 
'แกงเขียวหวานเป็นแกงไทยที่มีรสเผ็ดหวานเล็กน้อย มักใส่ไก่ มะเขือเปราะ และใบโหระพา นิยมกินคู่กับข้าวสวยหรือขนมจีน', 
'อนันต์ อร่อยดี', 'อาหารภาคกลาง', '2025-01-05'),

('ผัดไทย รสชาติระดับโลก', 
'ผัดไทยเป็นอาหารจานเส้นที่มีชื่อเสียงไปทั่วโลก ทำจากเส้นจันท์ผัดกับไข่ เต้าหู้ กุ้งแห้ง และซอสเปรี้ยวหวาน โรยถั่วลิสงและมะนาว', 
'วราภรณ์ ชวนชิม', 'อาหารจานด่วน', '2025-01-07'),

('ข้าวซอย อาหารเหนือหอมเครื่องแกง', 
'ข้าวซอยเป็นเมนูเส้นของภาคเหนือ ใช้เส้นบะหมี่ไข่ในน้ำแกงกะทิรสจัดจ้าน ใส่ไก่หรือลูกชิ้น โรยหอมเจียวและเส้นกรอบ', 
'ศุภชัย ลิ้มลอง', 'อาหารภาคเหนือ', '2025-01-10'),

('ลาบหมู อาหารอีสานรสจัด', 
'ลาบหมูเป็นอาหารอีสานยอดนิยม ทำจากหมูสับปรุงรสด้วยน้ำมะนาว ข้าวคั่ว พริกป่น หอมแดง และใบสะระแหน่ กินคู่ผักสด', 
'มนัส แซ่บเวอร์', 'อาหารภาคอีสาน', '2025-01-12'),

('ส้มตำไทย แซ่บถึงใจ', 
'ส้มตำไทยคือสลัดมะละกอที่มีรสหวาน เปรี้ยว เค็ม เผ็ดจากพริก น้ำมะนาว น้ำปลา และน้ำตาลปี๊บ เพิ่มความอร่อยด้วยกุ้งแห้งและถั่วลิสง', 
'สายฝน ตำแหลก', 'อาหารภาคอีสาน', '2025-01-14'),

('แกงส้มผักรวม เปรี้ยวจี๊ดโดนใจ', 
'แกงส้มเป็นแกงไทยรสเปรี้ยว เผ็ด ใช้น้ำพริกแกงส้มและมะขามเปียก ต้มกับผักหลากชนิด เช่น ดอกแค มะละกอ และกุ้งสด', 
'ณรงค์ เปรี้ยวจัด', 'อาหารภาคใต้', '2025-01-17'),

('หมูกรอบจิ้มซีอิ๊วดำ', 
'หมูกรอบเป็นเมนูที่ทำจากหมูสามชั้นทอดจนหนังกรอบ เสิร์ฟกับซีอิ๊วดำหวานและพริกน้ำส้ม นิยมกินคู่กับข้าวสวยหรือก๋วยจั๊บ', 
'อารีย์ เค็มกรอบ', 'สตรีทฟู้ด', '2025-01-20'),

('ไก่ย่างหนังกรอบสูตรอีสาน', 
'ไก่ย่างแบบอีสานใช้การหมักเครื่องเทศจนเข้าเนื้อ แล้วย่างจนหนังกรอบ เสิร์ฟกับข้าวเหนียวและน้ำจิ้มแจ่วรสแซ่บ', 
'วิทยา ย่างเก่ง', 'อาหารภาคอีสาน', '2025-01-22'),

('ขนมจีนน้ำยาใต้ หอมเครื่องแกง', 
'ขนมจีนน้ำยาใต้มีรสเผ็ดจัดจากพริกแห้ง ขมิ้น ตะไคร้ และกะทิ นิยมใส่เนื้อปลา แล้วราดลงบนเส้นขนมจีน พร้อมผักแนม', 
'ปาริชาติ เผ็ดแรง', 'อาหารภาคใต้', '2025-01-25'),

('ผัดกะเพรา ราชาอาหารจานด่วน', 
'ผัดกะเพราเป็นเมนูจานด่วนยอดนิยม ใช้เนื้อสัตว์ผัดกับพริก กระเทียม และใบกะเพรา มักเสิร์ฟกับข้าวและไข่ดาว', 
'ณัฐพล กะเพราเดือด', 'อาหารจานด่วน', '2025-01-27'),

('แกงมัสมั่น รสชาติเข้มข้น', 
'แกงมัสมั่นเป็นแกงไทยสไตล์อินเดีย ใช้เครื่องเทศหลากชนิดและกะทิเข้มข้น นิยมใส่เนื้อวัวหรือไก่ และมันฝรั่ง', 
'ทิพย์ เก่งแกง', 'อาหารพื้นบ้าน', '2025-01-29'),

('ยำวุ้นเส้นทะเล รสจัดจ้าน', 
'ยำวุ้นเส้นเป็นอาหารยำยอดฮิต ใส่กุ้ง ปลาหมึก หมูสับ และผักหลากชนิด ปรุงรสเปรี้ยว หวาน เค็ม เผ็ดแบบไทยๆ', 
'สุกัญญา ยำเก่ง', 'อาหารทะเล', '2025-02-01'),

('ข้าวเหนียวมะม่วง ของหวานคู่ชาติ', 
'ข้าวเหนียวมูนหอมกะทิ เสิร์ฟคู่กับมะม่วงสุกหวานฉ่ำ เป็นของหวานที่คนไทยและชาวต่างชาติหลงรัก', 
'สุรชัย หวานมัน', 'ของหวาน', '2025-02-04'),

('ปอเปี๊ยะทอดไส้ผัก', 
'ปอเปี๊ยะทอดเป็นของว่างที่นิยมมาก ไส้ผักหรือหมูสับห่อด้วยแป้งบางๆ แล้วนำไปทอดจนกรอบ เสิร์ฟพร้อมน้ำจิ้มเปรี้ยวหวาน', 
'อรนุช ทอดกรอบ', 'ของกินเล่น', '2025-02-07'),

('เต้าหู้ทอดน้ำจิ้มถั่ว', 
'เต้าหู้ทอดกรอบเสิร์ฟพร้อมน้ำจิ้มถั่วบดหวานๆ เป็นของกินเล่นหรือเมนูเจที่อร่อยและมีโปรตีนสูงจากพืช', 
'สุภาวดี เจสุขใจ', 'อาหารมังสวิรัติ', '2025-02-10'),

('ขนมเบื้องไทยโบราณ', 
'ขนมเบื้องเป็นขนมไทยโบราณที่มีทั้งไส้หวานและไส้เค็ม แป้งกรอบ ไส้ไข่แดงฝอยหรือไส้กุ้งแห้ง ทำให้รสชาติเข้มข้นและหอมหวาน', 
'บุษบา หวานกรอบ', 'ขนมไทย', '2025-02-12'),

('บัวลอยไข่หวาน', 
'บัวลอยทำจากแป้งข้าวเหนียวนุ่มๆ ในกะทิหอมหวาน นิยมใส่ไข่หวานเพิ่มความหอมและมัน กลายเป็นของหวานที่อบอุ่นหัวใจ', 
'สมบัติ ละมุนลิ้น', 'ของหวาน', '2025-02-15'),

('ไอศกรีมกะทิสด', 
'ไอศกรีมกะทิสดทำจากกะทิเข้มข้น ไม่ใส่นมวัว นิยมเสิร์ฟพร้อมถั่วลิสง ข้าวเหนียว หรือขนมปัง เป็นเมนูดับร้อนแบบไทยๆ', 
'เยาวลักษณ์ เย็นใจ', 'อาหารสุขภาพ', '2025-02-17');


-- Create ArticleTags Junction Table for Thai Articles
CREATE TABLE thai_article_tags (
    article_id INTEGER NOT NULL,
    tag_id INTEGER NOT NULL,
    PRIMARY KEY (article_id, tag_id),
    FOREIGN KEY (article_id) REFERENCES thai_articles(article_id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags_thai(tag_id) ON DELETE CASCADE
);

-- Associate Thai Articles with Tags
INSERT INTO thai_article_tags (article_id, tag_id) VALUES
(1, 1), -- First Thai article with tag 1
(1, 2), -- First Thai article with tag 2
(2, 3), -- Second Thai article with tag 3
(3, 4), -- Third Thai article with tag 4
(4, 1), -- Fourth Thai article with tag 1
(5, 2); -- Fifth Thai article with tag 2

-- Sample Full-Text Search Queries for Thai

-- 1. Basic Thai full-text search (using 'simple' configuration)
-- SELECT title, author
-- FROM thai_articles
-- WHERE search_vector @@ to_tsquery('simple', 'การพัฒนา');

-- 2. Ranking Thai results by relevance
-- SELECT title, author, ts_rank(search_vector, query) AS rank
-- FROM thai_articles, to_tsquery('simple', 'เว็บ & พัฒนา') query
-- WHERE search_vector @@ query
-- ORDER BY rank DESC;

-- 3. Highlighting Thai search results
-- SELECT title, author, 
--   ts_headline('simple', content, to_tsquery('simple', 'ฐานข้อมูล'),
--     'StartSel = <mark>, StopSel = </mark>, MaxWords=35, MinWords=15')
-- FROM thai_articles
-- WHERE search_vector @@ to_tsquery('simple', 'ฐานข้อมูล');
