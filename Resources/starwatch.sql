-- phpMyAdmin SQL Dump
-- version 4.5.4.1deb2ubuntu2.1
-- http://www.phpmyadmin.net
--
-- Host: localhost
-- Generation Time: Jul 08, 2019 at 10:43 AM
-- Server version: 10.0.38-MariaDB-0ubuntu0.16.04.1
-- PHP Version: 7.0.33-0ubuntu0.16.04.5

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `koala`
--

-- --------------------------------------------------------

--
-- Table structure for table `sb_accounts`
--

CREATE TABLE IF NOT EXISTS `sb_accounts` (
  `server` int(11) NOT NULL DEFAULT '1',
  `name` varchar(64) NOT NULL,
  `password` text NOT NULL,
  `is_admin` tinyint(1) NOT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT '1',
  `last_seen` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`name`),
  KEY `server` (`server`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_bans`
--

CREATE TABLE IF NOT EXISTS `sb_bans` (
  `ticket` bigint(20) NOT NULL AUTO_INCREMENT,
  `server` int(11) NOT NULL,
  `uuid` varchar(64) DEFAULT NULL,
  `ip` varchar(64) DEFAULT NULL,
  `moderator` text,
  `reason` text,
  `date_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `date_expired` datetime DEFAULT NULL,
  PRIMARY KEY (`ticket`),
  KEY `server` (`server`)
) ENGINE=InnoDB AUTO_INCREMENT=4307 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_chat`
--

CREATE TABLE IF NOT EXISTS `sb_chat` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `session` bigint(20) NOT NULL,
  `content` text NOT NULL,
  `content_clean` text NOT NULL,
  `location` text,
  `date_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `session` (`session`)
) ENGINE=InnoDB AUTO_INCREMENT=568 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_protections`
--

CREATE TABLE IF NOT EXISTS `sb_protections` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `name` text NOT NULL,
  `whereami` varchar(128) NOT NULL,
  `mode` enum('BLACKLIST','WHITELIST') NOT NULL,
  `allow_anonymous` tinyint(1) NOT NULL DEFAULT '0',
  `date_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `whereami` (`whereami`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_protections_accounts`
--

CREATE TABLE IF NOT EXISTS `sb_protections_accounts` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `protection` bigint(20) NOT NULL,
  `account` varchar(64) NOT NULL,
  `reason` text,
  `date_created` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `protection` (`protection`),
  KEY `account` (`account`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_servers`
--

CREATE TABLE IF NOT EXISTS `sb_servers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` text NOT NULL,
  `allow_anonymous_connections` tinyint(1) NOT NULL,
  `allow_assets_mismatch` tinyint(1) NOT NULL,
  `max_players` int(11) NOT NULL,
  `game_bind` varchar(255) NOT NULL DEFAULT '*',
  `game_port` int(11) NOT NULL DEFAULT '21025',
  `query_bind` varchar(255) NOT NULL DEFAULT '*',
  `query_port` int(11) NOT NULL DEFAULT '21026',
  `rcon_bind` varchar(255) NOT NULL DEFAULT '*',
  `rcon_port` int(11) NOT NULL DEFAULT '21024',
  `rcon_password` text NOT NULL,
  `rcon_timeout` int(11) NOT NULL,
  `json` text NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_sessions`
--

CREATE TABLE IF NOT EXISTS `sb_sessions` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `uptime` bigint(20) NOT NULL DEFAULT '0',
  `cid` int(11) NOT NULL,
  `ip` varchar(64) NOT NULL,
  `uuid` varchar(64) DEFAULT NULL,
  `username` text NOT NULL,
  `username_clean` text,
  `account` varchar(64) DEFAULT NULL,
  `date_joined` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `date_left` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `account` (`account`),
  KEY `uptime` (`uptime`)
) ENGINE=InnoDB AUTO_INCREMENT=116 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_uptime`
--

CREATE TABLE IF NOT EXISTS `sb_uptime` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `date_started` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `date_ended` datetime DEFAULT NULL,
  `reason` text,
  `last_log` text NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=118 DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Table structure for table `sb_worlds`
--

CREATE TABLE IF NOT EXISTS `sb_worlds` (
  `whereami` varchar(128) NOT NULL,
  `seed` bigint(20) NOT NULL,
  `x` bigint(20) NOT NULL,
  `y` bigint(20) NOT NULL,
  `z` bigint(20) NOT NULL,
  `name` text NOT NULL,
  `name_clean` text NOT NULL,
  `description` text NOT NULL,
  `size` text NOT NULL,
  `type` text NOT NULL,
  `biome` text NOT NULL,
  `day_length` float NOT NULL,
  `threat_level` int(11) NOT NULL,
  PRIMARY KEY (`whereami`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `sb_chat`
--
ALTER TABLE `sb_chat`
  ADD CONSTRAINT `sb_chat_session_const` FOREIGN KEY (`session`) REFERENCES `sb_sessions` (`id`) ON DELETE CASCADE ON UPDATE NO ACTION;

--
-- Constraints for table `sb_protections_accounts`
--
ALTER TABLE `sb_protections_accounts`
  ADD CONSTRAINT `sb_protections_accounts_acc_constant` FOREIGN KEY (`account`) REFERENCES `sb_accounts` (`name`) ON DELETE CASCADE ON UPDATE CASCADE,
  ADD CONSTRAINT `sb_protections_accounts_constant` FOREIGN KEY (`protection`) REFERENCES `sb_protections` (`id`) ON DELETE CASCADE ON UPDATE NO ACTION;

--
-- Constraints for table `sb_sessions`
--
ALTER TABLE `sb_sessions`
  ADD CONSTRAINT `sb_sessions_uptime_const` FOREIGN KEY (`uptime`) REFERENCES `sb_uptime` (`id`) ON DELETE CASCADE ON UPDATE NO ACTION;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
