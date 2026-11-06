START TRANSACTION;
CREATE TABLE `ErrosCatalogo` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Codigo` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `Nome` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Descricao` text CHARACTER SET utf8mb4 NOT NULL,
    `ExemploEsperado` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `ExemploResposta` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `ObservacaoPedagogica` text CHARACTER SET utf8mb4 NULL,
    `SerieAno` varchar(200) CHARACTER SET utf8mb4 NULL,
    `AutorId` int NOT NULL,
    `Ativo` tinyint(1) NOT NULL,
    `DataCriacao` datetime(6) NOT NULL,
    `DataAlteracao` datetime(6) NOT NULL,
    CONSTRAINT `PK_ErrosCatalogo` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ErrosCatalogo_Usuarios_AutorId` FOREIGN KEY (`AutorId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ErrosCatalogo_AutorId` ON `ErrosCatalogo` (`AutorId`);

CREATE UNIQUE INDEX `IX_ErrosCatalogo_Codigo` ON `ErrosCatalogo` (`Codigo`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261106051829_PublicarErroMarketplace', '9.0.11');

COMMIT;
