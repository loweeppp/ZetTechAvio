import './Footer.css';

export default function Footer() {
    return (
        <footer className="footer">
            <div className="footer-content">
                <div className="footer-logo"><img src="/routing-2.png" alt="ZetTechAvio" className="imagelogo" />
                    <span>ZetTechAvio</span>
                </div>
                <div className="footer-grid">
                    <div className="footer-column">

                        <h3 className="footer-title">ИНФОРМАЦИЯ</h3>
                        <p className="footer-text">
                            Веб-сервис для поиска и покупки авиабилетов. Форма поиска представляет формы
                            поиска по городу отправления/прибытия и дате, позволяет сразу рейсов, оформить покупку и
                            получить электронный билет.
                        </p>
                    </div>

                    <div className="footer-column">
                        <h3 className="footer-title">НАВИГАЦИЯ</h3>
                        <ul className="footer-links">
                            <li><a href="#">Домашняя</a></li>
                            <li><a href="#">Services</a></li>
                            <li><a href="#">Project</a></li>
                        </ul>
                    </div>

                    <div className="footer-column">
                        <h3 className="footer-title">СВЯЗАТЬСЯ С НАМИ</h3>
                        {/* className вызывает ошибку GET http://localhost:3000/routing-2.png NS_BINDING_ABORTED*/}
                        <div className="footer-contacts">
                            <div className="contact-item">
                                <svg className="icon-tiny" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                    <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
                                    <polyline points="22,6 12,13 2,6" />
                                </svg>
                                <span>Lumbung.tidap@esi.Java</span>
                            </div>
                            <div className="contact-item">
                                <svg className="icon-tiny" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                                    <path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72 12.84 12.84 0 0 0 .7 2.81 2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45 12.84 12.84 0 0 0 2.81.7A2 2 0 0 1 22 16.92z" />
                                </svg>
                                <span>Hallo@moto.com</span>
                            </div>
                        </div>
                        <div className="footer-subscribe">
                            <input type="email" className="input-footer" placeholder="Email Address" />
                            <button className="btn-subscribe">SUBSCRIBE</button>
                        </div>
                    </div>
                </div>

                <div className="footer-bottom">
                    <div>© 2025 ZetTechAvio. Все права защищены.</div>
                    <div className="footer-legal">
                        <a href="#">DISCLAIMER</a>
                        <a href="/privacy-policy" target="_blank" rel="noopener noreferrer">ПОЛЬЗОВАТЕЛЬСКОЕ СОГЛАШЕНИЕ</a>
                    </div>
                </div>
            </div>
        </footer>
    );
}